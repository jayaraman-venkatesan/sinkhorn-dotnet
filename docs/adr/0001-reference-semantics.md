# Preserve reference behavior and assess results explicitly

The library ports the pinned Basic and LogDomain update and stopping behavior
without silent normalization, support reduction, solver substitution, or
automatic fallback. Validation and explicit result assessment surround that
calculation instead: this exposes inconvenient numerical failures while
preserving reproducibility and making limitations visible to callers.
