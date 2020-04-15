﻿using Genometric.TVQ.API.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Genometric.TVQ.API.Analysis
{
    public class CitationChange
    {
        // TODO: This class and the methods leveraging it shall be re-implemented/designed/thought.
        public class Number
        {
            // Number of citations per year.
            public double Count { set; get; }

            // Cumulative number of citations in a given year.
            public double CumulativeCount { set; get; }
        }

        public enum DateNormalizationType { ByYear, ByDay };

        public double DaysOffset { set; get; }

        public double YearsOffset { set; get; }

        public double CitationCount { set; get; }

        public double Average { set; get; }

        private SortedSet<double> Citations { set; get; }

        public SortedDictionary<double, Number> CitationsNormalizedByDays { get; }

        public SortedDictionary<double, Number> CitationsNormalizedByYears { get; }

        public double Min
        {
            get { return MathNet.Numerics.Statistics.Statistics.Minimum(Citations); }
        }

        public double Max
        {
            get { return MathNet.Numerics.Statistics.Statistics.Maximum(Citations); }
        }

        public double Median
        {
            get { return MathNet.Numerics.Statistics.Statistics.Median(Citations); }
        }

        public double LowerQuartile
        {
            get { return MathNet.Numerics.Statistics.Statistics.LowerQuartile(Citations); }
        }

        public double UpperQuartile
        {
            get { return MathNet.Numerics.Statistics.Statistics.UpperQuartile(Citations); }
        }

        private double _gainScore = double.NaN;
        public double GainScore
        {
            get
            {
                if (double.IsNaN(_gainScore))
                    _gainScore = GetGainScore();
                return _gainScore;
            }
        }

        public CitationChange()
        {
            Citations = new SortedSet<double>();
            CitationsNormalizedByDays = new SortedDictionary<double, Number>();
            CitationsNormalizedByYears = new SortedDictionary<double, Number>();
        }

        public CitationChange(double daysOffset, double count)
        {
            DaysOffset = daysOffset;
            CitationCount = count;
            Citations = new SortedSet<double>();
        }

        public void AddRange(
            ICollection<Citation> citations,
            DateTime? dateAddedToRepository,
            IEnumerable<double> points)
        {
            Contract.Requires(citations != null);
            Contract.Requires(dateAddedToRepository != null);
            Contract.Requires(points != null);

            AddRange(citations, dateAddedToRepository);

            Normalize(CitationsNormalizedByDays);
            Normalize(CitationsNormalizedByYears);

            Interpolate(CitationsNormalizedByDays, points);
            Interpolate(CitationsNormalizedByYears, points);
        }

        private void AddRange(
            ICollection<Citation> citations,
            DateTime? dateAddedToRepository)
        {
            double daysOffset;
            double yearsOffset;
            foreach (var citation in citations)
            {
                daysOffset = (citation.Date - dateAddedToRepository).Value.Days;
                if (CitationsNormalizedByDays.ContainsKey(daysOffset))
                {
                    CitationsNormalizedByDays[daysOffset].Count += citation.Count;
                    CitationsNormalizedByDays[daysOffset].CumulativeCount += citation.AccumulatedCount;
                }
                else
                    CitationsNormalizedByDays.Add(daysOffset, new Number() { Count = citation.Count, CumulativeCount = citation.AccumulatedCount });

                // The average number of days per year is 365 + ​1⁄4 − ​1⁄100 + ​1⁄400 = 365.2425
                // REF: https://en.wikipedia.org/wiki/Leap_year
                yearsOffset = (citation.Date - dateAddedToRepository).Value.TotalDays / 365.2425;
                if (CitationsNormalizedByYears.ContainsKey(yearsOffset))
                {
                    CitationsNormalizedByYears[yearsOffset].Count += citation.Count;
                    CitationsNormalizedByYears[yearsOffset].CumulativeCount += citation.AccumulatedCount;
                }
                else
                    CitationsNormalizedByYears.Add(yearsOffset, new Number() { Count = citation.Count, CumulativeCount = citation.AccumulatedCount });
            }
        }

        private void Normalize(SortedDictionary<double, Number> citations)
        {
            var minBeforeDate = citations.First().Key;
            var maxAfterDate = citations.Last().Key;
            var deltaDate = maxAfterDate - minBeforeDate;

            // Normalize citation count.
            var dates = new double[citations.Keys.Count];
            citations.Keys.CopyTo(dates, 0);
            foreach (var date in dates)
            {
                citations[date].Count /= deltaDate;
                citations[date].CumulativeCount /= deltaDate;
            }

            // Min-Max normalize date.
            foreach (var date in dates)
            {
                double normalizedDate;
                if (date <= 0)
                    normalizedDate = (-1) * ((date - maxAfterDate) / (minBeforeDate - maxAfterDate));
                else
                    normalizedDate = (date - minBeforeDate) / (maxAfterDate - minBeforeDate);

                citations.Add(normalizedDate, citations[date]);
                citations.Remove(date);
            }
        }

        private void Interpolate(SortedDictionary<double, Number> citations, IEnumerable<double> points)
        {
            // At least two items are required for interpolation.
            if (citations.Count < 2)
                return;

            var countsSpline = MathNet.Numerics.Interpolate.Linear(citations.Keys, citations.Values.Select(x => x.Count));
            var cumulativeCountsSpline = MathNet.Numerics.Interpolate.Linear(citations.Keys, citations.Values.Select(x => x.CumulativeCount));

            citations.Clear();
            double y1;
            double y2;
            foreach (var x in points)
            {
                y1 = countsSpline.Interpolate(x);
                if (y1 < 0) y1 = 0;

                y2 = cumulativeCountsSpline.Interpolate(x);
                if (y2 < 0) y2 = 0;

                citations.Add(x, new Number() { Count = y1, CumulativeCount = y2 });
            }
        }

        public void AddCitationCount(double count)
        {
            Citations.Add(count);
        }

        public void AddCitationCount(List<double> counts)
        {
            Citations.UnionWith(counts);
        }

        public void RemoveOutliers()
        {
            Citations = OutliersRemoval.Remove(Citations);
        }

        public void MinMaxNormalize(double min, double max)
        {
            var normalizedCitationCount = new SortedSet<double>();
            foreach (var count in Citations)
                normalizedCitationCount.Add((count - min) / (max - min));
            Citations = normalizedCitationCount;
        }

        private double GetGainScore()
        {
            double score = 0.0;
            double normalizedDate;

            foreach (var point in CitationsNormalizedByYears)
            {
                normalizedDate = ZeroOneNormalize(point.Key);

                // Multiply citation count by _logit_ of 0-1 normalized date; 
                // hence citation counts of when the tool was NOT added to the 
                // repository will decrease the score (which decreases more as 
                // the citation count belongs to a time older date as when the 
                // tool was added to the repository) and citation counts of 
                // after when the tool was added to the repository, increase the 
                // score (effect of which increase the date of citation is more 
                // current.
                score += point.Value.Count * (-Math.Log((1 / normalizedDate) - 1, Math.E));
            }

            return score;
        }

        private double ZeroOneNormalize(double x, double epsilon = 1e-6)
        {
            return (x - (-1 - epsilon)) / ((1 + epsilon) - (-1 - epsilon));
        }
    }
}
