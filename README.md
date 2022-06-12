## [Archived]

I discontinue work on the BWDPerf project in place of [incan74re](https://github.com/Mitiko/incan74re/) - a rust re-write (for speed) and with a smaller more intentional scope.

# BWDPerf

![.NET](https://github.com/Mitiko/BWDPerf/workflows/.NET/badge.svg)

BWD is a dictionary coder, that provides close to optimal dictionaries and patterned words.

## Structure of the repository

The repository contains a very efficient pipeline for compression, that will later be seperated in a different repo and (currently) no implementation of BWD. My goal is to implement BWD more efficiently than my PoC local project, hence the Perf in BWDPerf.

## Explanation

You'll find a very thorough explanation of how the algorithm works
[here](https://mitiko.github.io/BWDPerf)
