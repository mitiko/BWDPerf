# BWD

Best Word Dictionary

# Introduction

There are 3 main ways to compress data:
1. Dictionary coding
2. Entropy coding + modeling
3. Block sorting algorithms

## Block sorting

Block sorting algorithms don't take advantage of the sequential nature of data and are usually used for more unpredictable datasets.
My goal is to compress text, so I'll focus on that.

## Entropy coding

Entropy coders compress data using variable length codes - assigning shorter codes to more common data. These have been explored thoroughly and state of the art implementations are proven to be asymptotically optimal. Improvements here are mostly speed related.

Modeling is the main weapon for modern coders aiming at the highest ratio. The idea is to predict the next symbol more accurately, which in turn will be reflected in a smaller code from the entropy coder.

## Dictionary coding

Dictionary coders help eliminate bigger redundancies in data, which low order entropy coders can't do themselves and are insanely fast and computationally cheap to decode.
In fact, most compressors optimized for decompression speed have a dictionary coder as their backbone. LZ4 doesn't even use the overhead of an entropy coder and instead uses more time to focus on more optimal parsing.

Most compressors combine a dictionary coding scheme with an entropy coder and use different optimizations for speed and compression ratio.

# Types of dictionary coders
There are 3 types of dictionary coders:
1. static dictionaries
2. semi-adaptive dictionaries
3. adaptive dictionaries


The idea of dictionary coding is to modify the alphabet of symbols occuring in the stream by replacing symbols that show up together frequently (words) with an index pointing to the word in the dictinary.

## Static dictionaries

Static dictionaries are the most basic type.
They're just a list of pre-set words that are likely to occur in the stream.
They're common for datasets like DNA sequences.
For example you might replace every 2 bases in a DNA sample with a new symbol, transforming the alphabet from 4 symbols to 4*4=16 symbols.
It's not required that a static dictionary fully covers the whole stream.

## Adaptive dictionaries

Adaptive dictionaries are the most popular ones.
You've seen them in the form of LZ77 and LZ78.
The idea is that instead of using a pre-set dictionary (a static dictionary) we use the already decoded stream as our dictionary.
We can encode repeating words as a pair of numbers, representing the last location this word was seen and its length.

The parameters influencing LZ77/78 are the window size and the greediness of the matching algorithm.
The theory behind LZ77/78 is based on graphs.
Text can be represented as a DAWG (link) where each edge is a match and edges are weighted by the bits needed to encode them.
LZ77/78 is most commonly used in speed optimized archivers, so it's common to prefer a smaller compression ratio (with greedier parsing) to insane speeds.

LZ77/78's main disadvantage is its relation with entropy coders.
Offset-length pairs lose their meaning when taken out of the context of the text, so they're harder to predict.
I'm not saying entropy coders hurt compression, no, they improve it quite a bit but there's just more meaning to be extracted from the data that we're not taking advantage of.

## Semi-adaptive dictionaries

Those are just static dictionaries but instead of being pre-set, the compressor looks at the data and tries to compute an optimal dictionary for that specific data.

The disadvantage is that there's great asymmetry in the compressor and decompressor.
Compression may take hours to compute a very good dictionary, while the decompressor purrs faster than any LZ77/78 implementation.
Again, there's an obvious tradeoff in compression time and ratio but decompression remains fast.

Semi-adaptive dictionaries, like static dictionaries have to add the dictionary to the stream so the fight with LZ77 is not really fair. Adding an entropy coder on top is how we capitalize on time spent compressing.

Semi-adaptive dictionaries are perfect for cold data storage, where compression speed doesn't matter and for small data blocks - where a single dictionary can be reused and updated over time.