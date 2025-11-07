# Commons Style Guide

# Strings
## Initialization
### Empty Strings
If a string must be instantiated empty but must immediately be assigned a proper value, use `string.Empty` instead of `""` for empty strings. Otherwise, if the string can be left empty, use `""` for empty strings.

# Documentation
## Phrasing
### Use of "must", "should", "could", etc.

#### Must
Explicitly required. **Will** cause issues if ignored.

e.g.: "The array parameter must not be null or empty."