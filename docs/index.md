# BWD

BWD stands for Best Word Dictionary.

# Progress

BWD started in September 2020, as a dictionary coder, aimed at compressing genetic data.
As a proof of concept project, it managed to get close to gzip on small text files (not genetic data) with emulated entropy coding, thus prompting me to write a more efficient implementation.

BWDPerf is the continuation of the project.
It is still experimental and a lot of frequent changes are happenning to the core algorithm.
The goal is to settle on what works and what slows us down, then implement it more efficiently in C++.

Even if it doesn't prove to be way better than LZ77, I will come out with more knowledge of how dictionary coding techniques work in essence.

# The idea

BWD is a dictionary transform that replaces strings of symbols with an index to a static dictionary.  
Unlike LZ77/78 it computes a dictionary for the whole block and has to encode the dictionary in the compressed file.

The idea behind BWD is to compute an optimal dictionary.
According to this paper (include link), the problem of parsing the text for such global dictionary techniques is related to the vertex cover problem, which is an NP-hard problem.  
I haven't been able to prove it myself and there's no good reference to this claim in the paper but it intuitively makes sense.

## History
In the world of data compression, new improvements are rare, perhaps because of the sheer difficulty and beauty in data compression.  
Entropy coding is proven to be asymptotically optimal and the next best way to beat entropy is dictionary coding.
Since the creation of the LZ77 algorithm by A.Lempel and J. Ziv there hasn't really been any new foundationally different approach to replacing strings of symbols with new symbols.
I decided to think over that. One night I went for a run and got the idea of fully covering the text with small stickers - words.
I started working on it and the foundations were quickly formed:

1. The dictionary must have be ordered.
2. Words must be limited in length.
3. Words must be ranked.

As I dug deeper, 2) and 3) are avoidable but are rules, required for keeping the compression feasably fast.
The first thing I had a problem with was overlapping words - if 2 words overlap we have to choose one over the other, and the best way to do that is to add the requirement that the dictionary be ordered. This is not a problem for decompression - after we've chosen the best dictionary and encoded the data, we can sort it alphabetically. The requirement holds while compressing and during parsing.

The next rule of limiting the word length is pretty self-explanatory. The longer words we have, the less likely they are to occur and the more space we're taking in the dictionary section of the compressed file. Unlike LZ77 optimal parsing, where longer matches mean more compression, here we always statically store the first occurence of every word.

How do we order the dictionary? The easiest way to order any set is to define a characteristic by which it can be compared to other entities in the set. Thus we define the rank of a word as a metric of how useful it is to aid the compression of the file. There's other more optimal ways to choose the best dictionary but those are ultimately NP-hard.  
For example, we can try every possible ordering. This takes O(w!) time, where w is the number of words.
And to your best surprise, the word count is actually O(mn) where n is the stream size and m is the max word length.

Optimality is unfeasable and frankly it won't be the compression won't be that much better.

# Inner workings

I started out by finding a good ranking function. What's a good metric for how good a word is? Intuitively I went for a simple `(wordCount - 1) * (wordLength)`. The idea behind it being that we're searching for more common words that are as long as possible, substracting 1 because we're essentially not compressing the first occurence of any word.

So what the first PoC project did was to create a list with all possible words, sort them by their rank and eliminate the ones that will be unused.  
For example if we have the word "a", we can't have the word "ab" after it in the dictionary, as we've removed all the "a"-s from the stream already.  
I called this static elimination.
It's followed by dynamic elimination, where we runtime check if the word exists in the stream.

Looking back on it, this is wildly inefficent. But calculating the entropy over the new stream and seeing it being close to gzip and others, made me believe there's more to it than a toy. It could also only handle just a couple of kilobytes of data max, because of the gigantic allocations. I was storing each word as its individual string - that's stupid, don't do that for O(mn) strings.

The second problem I run into was how to handle the text ones we remove a word from it.
We can't just stitch the parts together, then it would be undecodable by the decompressor.
We split the whole text into multiple contexts (as I called them).
Then we just continue to encode, but note that words can't span between two contexts.
After reading some literature on LZ77 and LZ78, I realize this is the parsing stage of the compressor.

When I started BWDPerf, I improved the ranking first.
When we eliminate a word from the text, the contexts change and with it the count of some words and with it, their ranking.
This means, we can't just order the words by their rank and work on that ordering.
We only need the highest ranked word. Then we'll re-rank after each choosing of a word.

The final structure looked something like:
```
Compression:
while (not everything is encoded):
    count_words()
    rank_words()
    var word = ranks.max()
    split_by(word)
    add_to_dictionary(word)

Decompression:
while (there's data to read):
    index = read_byte()
    write(dictionary[index])
```

The last big problem left to solve was the limited dictionary size.
When BWD isn't followed by an entropy coder, it must encode the indexes to the dictionary with a fixed number of bits.
If we let the dictionary size change mid-compression, the rankings wouldn't be accurate.
To fix that, I decided to add an escape token, encoded as a word.
You'll see it in my code as SToken.
We add words to the dictionary until just one space is left.
It's reserved for the s token.
Everything left to encode gets summed up into one string and contexts are seperated by an escape character 0xff.
This one big string - the STokenData is stored in the dictionary section.
When decoding, if the s token is encountered, it copies bytes from the dictionary section, until an escape character is hit.

This method made it hard to keep all words into one data structure and introduced some problems because of the mismatch in parsing between the decompressor and compressor. Ultimately, it helped me find some bugs that would have otherwise remained hidden for at least the next 10 versions.

# Optimizations made

Since the first more efficient implementation I have made lots of changes.
I'm bouncing between memory constraints and speed.
Bigger files are too slow to compute in one block, so I make the algorithm faster by allocating more memory but now even larger files use too much memory, so I optimize the algorithm to use better data structures and allocate less.
It's been a constant battle but I'm still seeing a lot places where improvements can be made. My goal for now is to compute the dictionary for enwik8 in one block in under 1 minute.

Since I'm constantly developing and changing stuff, I haven't had the time to make a release. Still, when I hit enwik8 in under a minute, I'll drop it as v1.
As for now, here's a basic changelog:

## v0.2

The first change I did that improved everything was to store matches.
After we split the data by a word, the counts of a lot of words change.
We need the counts of words for any ranking function, so insted of recounting, we should only update the rankings of words near a split location.

Easy in theory but in practice, quite difficult. Eventually, I had the idea of a matrix (currently in code referred to as the wordRef matrix beacuse it shows the first reference to this word), which keeps track of how words are linked together. Like a graph. It takes 4n bytes and simultaneously keeps track of where data is split, which is also pretty cool.
We can also modify the hashtable for words and use words as (location, length) pairs, which simplifies the hashing.

## v0.3

Eventually I was able to move to testing on enwik4 and the transform took about 90 seconds on it. enwik5 would need more than 10 minutes and I tried once with enwik6, which didn't finish in under 6 hours on my laptop.

I identified the problem as the way I'm searching for matches - it takes O(m n^2) time.
I searched for better string searching algorithms and the ony one that really works is the FM-index.
It can do search in O(m) time (where m is the max string length).

Looking into the FM-index, though, I realize that the underlying suffix array is where the magic's at. And we can directly use the suffix array in more places to speed up compression.
I copied a simple O(n logn) implementation and got it to work. I then debugged this implementation for 8 hours straight to find a stupid mistake I made in copying arrays.
I'm grateful I made that mistake as well, as I actaully spent time to understand how the algorithm works and found out I can speed up initialization to O(n log m) (where m is the max word size).

Currently, the suffix array computation is the least expensive one speedwise. A lot of refactoring also took place.

## v0.3.1

I added an interface for ranking. This makes it easy to swap in and out new ranking algorithms.

I got motivated, since I found calculated the best ranking function.
I'll post my math here tommorow.

## v0.4 - working on it

The wordRef matching still takes too much time and I only need the counts of words.
We don't have to search every word for O(n log n) matching, since the suffix array basically provides us with a sorted list of all words.
We can enumerate over that to acquire word counts.

In v0.3 I didn't actually implement the feature of only updating the counts and ranks of words, since the wordRef matrix made it very convinient to do both in O(word count).


# Research

I'll post more about alternative ideas I came up with in the process for optimal parsing and for better compression, and modeling over the new alphabet.

Some of the imporatant ideas, shortly are:

## Patterns

We can use patterns as words. Imagine a regex "a*b". It can match "aab" and "abb".  
But it doesn't have to be a regex. The s token for example is also a pattern.

The abstract idea is that a pattern can match more words (the s token, matching all words of all lengths) but at the cost of having to complement each match with extra data to say which word it actually matched.

The complementary data to patterns has it's own context as well and should be predicted by a different set of models. For example, the pattern "<*+/>" (not real regex, just pseudocode) matching all xml tags.
The complementary data to those would be limited to a lot less words. In fact it would make some sense to dictionary encode the new complementary stream as well.

Another pattern would be the complementary sequence in DNA. Let's say we have the word "ACTG". We can match this pattern with "ACTG" and its complementary "TGAC", resulting in a complementary bit which differentiates between the two. This bistream is now very compressable, since one bit will be a lot more frequent. Order-0 models would actually work a lot better on it.

## Contexts

The idea of patterns sparked the idea of contexts. What if we create a graph of dictionaries - different contexts and we have an extra symbol to switch between them. There's a lot of potential in its abstract form.

A cool use of this would be to use multiple dictionaries created with multiple ranking algorithms and let a context mixer choose what dictionary/context to encode the next symbols with.

Btw, cool fact, this requires an on-line encoding method for dictionary coders, which exists but it's harder to implement.

We don't have to bound ourselves to dictionaries, either...