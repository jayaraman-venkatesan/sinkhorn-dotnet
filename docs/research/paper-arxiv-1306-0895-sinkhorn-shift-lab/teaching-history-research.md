# Sinkhorn teaching history: evidence and chapter outline

Research for ticket #17, verified 2026-09-19. Scope: historical attribution, accessible motivation, and teaching boundaries; no implementation or asset acquisition.

## Historical anchors

| Anchor | Safe teaching claim | Evidence and qualification |
| --- | --- | --- |
| Monge, 1781 | The classical earth-moving question asks how to rearrange equal amounts of material while minimizing transport cost. | The [SMF/BnF bibliography](https://smf.emath.fr/smf-dossiers-et-ressources/bibliographie-j-delon-bnf-2021) identifies Monge's memoir, pages 666–704, in the Academy volume for 1781. [Appell's original mathematical exposition preserved by NUMDAM](https://www.numdam.org/item/MSM_1928__27__1_0/) cites that volume and discusses the problem. Use “Monge's 1781 memoir”; do not invent a precise publication day or imply that he solved every modern OT problem. |
| Kantorovich, 1942 | Allowing a transport plan makes it possible to divide material from one source among multiple destinations. The finite problem becomes linear optimization. | [CERN's institutional record](https://cds.cern.ch/record/739801/) dates *On the translocation of masses* to 1942. [The publisher's later translation](https://link.springer.com/article/10.1007/s10958-006-0049-2) is dated 2006: do not confuse the translation with the original. For the discrete formulation, see Cuturi §2 below. |
| Sinkhorn, 1964; Sinkhorn–Knopp, 1967 | Alternating row and column scaling is older than modern machine-learning OT. Strictly positive square matrices can be scaled to doubly stochastic form; matrices containing zeros need additional conditions. | [Sinkhorn–Knopp's original 1967 paper, p.343](https://msp.org/pjm/1967/21-2/pjm-v21-n2-p14-s.pdf) states the positive-matrix result, credits Sinkhorn's earlier work, and gives conditions for nonnegative matrices. Its references identify the 1964 paper. [Original 1964 DOI](https://doi.org/10.1214/aoms/1177703591). The 1964 publisher page did not expose readable text; the 1967 primary paper supplies the checked mathematical statement. |

These anchors are selected teaching milestones, not a claim that this is the complete history of entropy regularization or iterative proportional fitting.

## What the modern papers contribute

[Cuturi (2013), §§2–5](https://arxiv.org/pdf/1306.0895), connects entropy-regularized transport to matrix scaling and efficient matrix operations, including parallel GPU computation. For positive histograms and finite costs, the optimizer has the form `P = diag(u) K diag(v)`, with `K_ij = exp(-lambda C_ij)`; scaling enforces the requested marginals. His experiments compare computation and classification performance on MNIST. Present the contribution as a computational route that made regularized OT attractive for machine learning, not the invention of OT, entropy, or alternating scaling. Report speed gains as results of his benchmark setup, never a universal promise. His paper uses inverse regularization `lambda`; a modern `epsilon` teaching slider should explain `epsilon = 1/lambda`. Label the transport-only score separately from the full entropy-regularized objective.

[Feydy et al. (AISTATS 2019), Eq.3 and Theorem 1](https://proceedings.mlr.press/v89/feydy19a/feydy19a.pdf), studies the debiased quantity `S_e(a,b) = OT_e(a,b) - OT_e(a,a)/2 - OT_e(b,b)/2`. It removes self-comparison bias and bridges OT and kernel discrepancies. The paper proves positivity and related properties under stated kernel/cost and support assumptions, and supplies scalable numerical methods. It explicitly credits earlier work for the debiasing formula; do not say Feydy invented it in 2019. The [proceedings record](https://proceedings.mlr.press/v89/feydy19a.html) is 2019; the [arXiv preprint](https://arxiv.org/abs/1810.08278) began in 2018. A Sinkhorn divergence is not automatically a metric satisfying the triangle inequality. Do not silently replace Cuturi's reported score with this later divergence.

## Suggested chapter evidence outline

1. **Why distance between bins matters.** Use original synthetic histograms: a one-bin shift should look less disruptive than a far-away shift. This is a proposed teaching example, not a reproduced experiment.
2. **From piles to a transport table.** Introduce sources, destinations, unit costs, and a plan whose row/column totals conserve mass. Connect to the Monge/Kantorovich milestones above; explain a plan before displaying an optimization formula.
3. **Why soften the optimization?** Show an original small table with sharp versus diffuse plans. Explain that regularization changes the problem; it is not merely a faster exact solver for unregularized OT. Cite Cuturi.
4. **One scaling step at a time.** Animate row and column targets and their residuals. Credit the 1960s scaling work, then explain its 2013 computational use. Keep historical doubly stochastic row sums of one distinct from probability marginals summing to one overall.
5. **Which score are we plotting?** Separate transport cost, regularized objective, and debiased divergence. Use Feydy for the self-comparison correction and its assumptions.
6. **What engineers can use this for.** End with concrete examples and limits below; clearly identify proposed exercises.

## Practical uses and asset cautions

- **Color transfer:** represent each image by a distribution of RGB colors, then transport one palette toward another. The authors' [GeomLoss color-transfer example](https://www.kernel-operations.io/geomloss/_auto_examples/optimal_transport/plot_optimal_transport_color.html) demonstrates this directly. Color-space matching alone does not encode objects or spatial layout.
- **Image comparison/classification:** Cuturi's MNIST study supplies a real research example. Any fresh browser demonstration would be illustrative unless it reproduces the paper's complete protocol.
- **Point-cloud fitting:** Feydy's figures show distributions moving under different losses; an original synthetic two-dimensional example can teach why debiasing matters. Do not market the proposed lesson as a validated registration product.

Prefer newly drawn diagrams, synthetic points, and generated geometric palettes. A publicly readable PDF does not establish permission to reuse its figures, photographs, portraits, screenshots, or accompanying datasets. Before importing any asset, record its exact source, author, license/version, attribution, and modification requirements; paper, code, and example-image licenses may differ. Link to historical scans unless their reproduction terms have been checked. No external asset's reuse license was verified in this research pass.
