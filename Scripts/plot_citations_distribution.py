import numpy as np
from numpy import std
import os
import sys
import pandas as pd
from scipy.stats import ttest_rel, ttest_ind, pearsonr, ttest_1samp
from statistics import mean
from math import sqrt
from t_test_clustered_data import get_vectors
from plot_tool_pub import set_plot_style
from cluster import CLUSTERED_FILENAME_POSFIX
import matplotlib.pyplot as plt
from matplotlib.ticker import PercentFormatter, FormatStrFormatter, ScalarFormatter
from citation_growth_histogram import aggregate
from t_test_clustered_data import get_repo_name


def plot(ax, pre_citations, post_citations, plot_density, title, ylabel):
    counts, bins, patches = ax.hist(pre_citations, # label=labels, 
                                    label="Pre publication to repository citation count",
                                    bins = 30,
                                    edgecolor="#3498db",
                                    color="#3498db",
                                    #alpha=0.1,
                                    histtype="step",
                                    density=plot_density) # bins=bins, rwidth=0.65,
                                    #color = colors, align="left", histtype="bar")   # setting density to False will
                                                                                    # show count, and True will show
                                                                                    # probability.
    counts, bins, patches = ax.hist(post_citations, # label=labels,
                                    label="Post publication to repository citation count",
                                    bins = 30,
                                    edgecolor="#feb308",
                                    color="#feb308",
                                    fc=(0, 1, 0, 0.3),
                                    histtype="step",
                                    density=plot_density)

    ax.set_title(title)
    ax.set_xlabel("\nCitations count\n\n")

    if ylabel:
        ax.set_ylabel(ylabel)

    ax.set_yscale('log')
    if not plot_density:
        ax.yaxis.set_major_formatter(FormatStrFormatter('%d'))


def run(input_path, plot_density):
    files = []
    for root, dirpath, filenames in os.walk(input_path):
        for filename in filenames:
            if os.path.splitext(filename)[1] == ".csv" and \
               not os.path.splitext(filename)[0].endswith(CLUSTERED_FILENAME_POSFIX):
                files.append(os.path.join(root, filename))

    fig, axes = set_plot_style(1, len(files), 3, 16)

    ylabel = "Count"
    if plot_density:
        ylabel = "Probability"

    col_counter = 0
    for file in files:
        tools = pd.read_csv(file, header=0, sep='\t')
        _, pre_citations_vectors, post_citations_vectors, _, _, _, delta = get_vectors(tools)

        pre_citations = []
        for citation in pre_citations_vectors:
            pre_citations.append(np.max(citation))
        pre_citations = aggregate(pre_citations, 0, 500)

        post_citations = []
        for citation in post_citations_vectors:
            post_citations.append(np.max(citation))
        post_citations = aggregate(post_citations, 0, 500)

        plot(axes[col_counter], pre_citations, post_citations, plot_density, get_repo_name(file), ylabel if col_counter == 0 else None)
        col_counter += 1

    handles, labels = axes[-1].get_legend_handles_labels()
    fig.legend(handles, labels, loc='center', bbox_to_anchor=(0.410, 0.04), ncol=2, framealpha=0.0)

    image_file = os.path.join(input_path, 'citations_distribution.png')
    if os.path.isfile(image_file):
        os.remove(image_file)
    plt.savefig(image_file, bbox_inches='tight')
    plt.close()


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Missing input path.")
        exit()

    plot_density = False
    if len(sys.argv) >= 3:
        plot_density = sys.argv[2] == "True"

    run(sys.argv[1], plot_density)
