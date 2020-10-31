<head>
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/katex@0.10.2/dist/katex.min.css" integrity="sha384-yFRtMMDnQtDRO8rLpMIKrtPCD5jdktao2TV19YiZYWMDkUR5GQZR/NOVTdquEx1j" crossorigin="anonymous">
<script defer src="https://cdn.jsdelivr.net/npm/katex@0.10.2/dist/katex.min.js" integrity="sha384-9Nhn55MVVN0/4OFx7EE5kpFBPsEMZxKTCnA+4fqDmg12eCTqGi6+BB2LjY8brQxJ" crossorigin="anonymous"></script>
<script defer src="https://cdn.jsdelivr.net/npm/katex@0.10.2/dist/contrib/auto-render.min.js" integrity="sha384-kWPLUVMOks5AQFrykwIup5lo0m3iMkkHrD0uJ4H5cjeGihAutqP0yW0J6dpFiVkI" crossorigin="anonymous" onload="renderMathInElement(document.body);"></script>
</head>

# BWDPerf

BWD stands for Best Word Dictionary as it has the ability to be an optimal dictionary coder.

## Introduction

A dictionary coder encodes strings of characters (also called words) as individual symbols, that then can go through an entropy coder of choice.
There are 2 types of dictionary coders - static and dynamic.

| Static coders    | Dynamic coders |
| ---------------- | -------------- |
| Static coders encode their dictionary in the beggining of a file and don't change it. | Dynamic coders can adapt to the content and change their dictionaries while coding.
| They're usually used for pre-processing of text. | The LZ77 and LZ78 family are the most famous. They're proven to be asymptotically optimal at their job.

## BWD

BWD compresses data in blocks. With bigger blocks, the compression ratio imporves, the encoding speed increases, but the decoding speed decreases.
BWD can be considered a semi-dynamic dictionary. It adapts to the context (restricted to the block) but then emits a static [optimal](#3-optimality) or sub-optimal dictionary.  
What makes it able to perform better than the LZ family is that it can capture long repeated words in the very beggining of the stream, without trashing the entire dictionary of unused/useless/dead words. Asymptotically they're probably equal (not proved, yet), but what gives BWD an extra boost is [patterns](#4-patterns). And although it might seem tempting to skip and read how they work, I advise you not to.

### 0. Idea

The idea for BWD started from wanting to create a dictionary-like coder that will be able to take advantage of overlapping words. This proved to be stupid, but led me to the semi-dynamic structure used.

To ensure no words overlap, we must have an ordered dictionary.
This is because the dictionary `["the","en"]` will encode `"then"` as `[0]n`, while the dictionary `["en","the"]` will encode it as `th[0]`. The idea is to split the stream into smaller blocks (each representing a word) in the best way possible.

Working on how this might improve current dictionary coders (or more like whether it can beat them at all), I figured [patterns](#4-patterns) are possible.

Also sounds cool that you can say it doesn't work backwards and forwards, but rather form the middle-out haha.

### 1. Algorithm

As noted above, BWD needs an ordered dictionary. To creates such, we go through all possible words up to size \\(m\\):
\\({W = \{w : \|w\| < m\}}\\)  
Next we rank each word using a [ranking](#21-ranking) algorithm: 
\\({r(w) \in \R, \forall w\in W}\\).

We sort the words based on their ranking and remove all words that can't exist in the stream.
For example: `them` as a word can't exist in the dictionary if it follows `the`. This is not as simple as it sounds, we can't just remove words that are supersets of words higher up, for example:
The word `noted` might never appear after `as no` and `ted above` in some contexts, but not always as was the case for `the` and `them`.  
We can take advantage of that and remove all superset words \\(O(\|W\|^2)\\). And then go through the block to find words of the second kind. We call those static and dynamic elimination respectively.
What is left will be our dictionary.

You can see that the compression rate depends on the ranking used, while time depends on the block size used.

After a sample implementation of the algorithm, I noticed some words will change their ranking. This is because of words like `noted`, when ranking, their occurences in the stream will be counter, but after words that overlap `noted`, its count can change, and therefore it's ranking, which leads us to worse than [optimal](#3-optimality) compression.  
Instead, what I decided to do is choose the highest ranked word, split the block by this word, select all words that still exist and re-rank them.

### 2. Ordering

Choosing an ordered dictionary is in the heart of BWD. We can find an optimal one in \\({O(\|W\|!)}\\) time, but since \\(\|W\|\\) is on the order of \\({O(m^3({b\over m} - {2 \over 3}))}\\), for block size \\(b\\) and word limit \\(m\\), this impractical.

The next best thing to bruteforce is bubble sort. We start with some order and check if switching the places of 2 words will give us an advantage. This makes some assumptions of how ordered dictionaries work, which are incorrect, but still give a good sub-optimal order - for example some group of words can be better together, than individually, but since we're making moves individual moves, groups won't be preserved. Also checking for an advantage can't be practically done by applying the algorithm for each order - it's done by a cost function, which gets as close to the real thing as a ranking would - read below.

### 2.1 Ranking

Ranking the words to order them, gives them an individual value (which as explained above is not always the case, but workarounds are possible).

The most intuitive approach, and also my first idea, was to see how many characters we're cutting off.
We're essentially replacing a bunch of symbols with a single symbol. (The use of symbol, instead of character, or literal implies the connection to an entropy coder afterwards.)
So it's intuitive to start with:  
\\(r(w) = (\|w\| - 1) f(w),\\)  
where \\(f(w)\\) is the frequency in the input stream or occurences count. This ranking represents loss of information, just like entropy does - individual loss * frequency (or probability), so we must be on the right track.  
Next we notice that some really long strings start to get ranked highly, when in fact their frequency is low, and they seem not as usable. We can correct that by remembering the dictionary gets encoded to the output as well -> we should substract 1 occurence, because we're gaining as much information:  
\\(r(w) = (\|w\| - 1)f(w) - \|w\|\\)  
But we can modify all ranks by a constant +1 to improve readability and aesthetics:  
\\(r(w) = (\|w\| - 1)(f(w) - 1)\\)  
Very nice! But we're still not quite there.
The ranking should assume we're replacing each word with 1 symbol, but we're relating it to count of characters, which are 8 bits each => unintentionally it's like we've been replacing each word with 1 literal, not symbol, i.e. 1 byte.  
To fix that, we need to know how long the dictionary will be, therefore we must set a constant size for it, or try the algorithm for all dictionary sizes (not as impractical than \\(O(\|W\|!)\\))  
Either way, regardless of overall implementation of the algorithm, let's say we've chosen a dictionary size of \\(d = 2^r\\), where \\(r = log_2(d)\\) and represents our index size. Having a fixed index size of the dictionary (also called codeword for the word) stays blind to the ability of entropy coders to approximate entropy, **more research is needed to find an entropy based ranking algorithm.** Now we can calculate the loss, not in symbols, but bits:  
\\(r(w)=(f(w) - 1)(\|w\|bpc - r),\\)  
where \\(bpc\\) is bits per character and is 8 for text files and for example 2 for genetic data.
This is of yet, the best and most practical ranking algorithm.

There does exist a complication when choosing a good dictionary, though - it may not cover the whole stream. Some individual characters may be left floating around alone, or in small groups. To fix that, we reserve an extra pattern word, that matches all consecutive characters (the supplementary to such a word, also called the `<s>` token in most of my notes, are the characters themselves, followed by an escape symbol, that switches the context back to words, or in this scenario ends the block).

### 3. Optimality

As discussed above, optimality can be reached and will be reached in a finite amount of operations, but this remains impractical for now (quantum computers may give it a new breath, but until then...). When is optimality reached?
We have to max all our constants:  
\\(b=n, m=n,\\) ordering is done in \\(O(\|W\|!)\\) time, and all orderings are tried. This will for sure give us better results than most LZ implementations. Actually, before any proper testing (and with sub-optimal ordering) it is almost certain BWD is a better dictionary than most dynamic ones. When we take into account a complex group of patterns and context switching (both areas, where work is still needed), it will redefine the limits of a dictionary coder. More on that at [context splitting](#1b-context-splitting).

Let's assume some dictionary is being used. We can calculate the compression ratio, fairly easily by counting the bits in the input in 2 ways - by characters and by words. (Please contact me for a full proof and more of my notes.) We'll end up with the following:  
$$\Large
\gamma = \log_{\alpha}d \times { {1}\over{\sum\limits_{w \in W}p(w)\|w\| } }
$$
For an alphabet \\({A: \|A\| = \alpha,}\\) dictionary of size \\(d\\) and probability of word \\(\large {p(w): \sum\limits_{w \in W}p(w)=1}\\)  
Which is oddly similiar to entropy, except we're dividing rather than substracting. The probability times the word length corresponds to the probability times optimal code length in entropy.

If we somehow convert this metric to a ranking function, we'll have an optimality constraint and this will be proof that BWD gets asymptotically close to entropy, moreover it will prove BWD + entropy coder gets asymptotically closer to the compression limit than any LZ implementation paired with an entropy coder.

### 4. Patterns

Patterns are a new type of word, that can match multiple unique strings.
For example the pattern `the*` can match `"the\x20"`, `"them"`, `"then"`. It can also match parts of words: `"there"`, `"other"`, `"thier"`, `"weather"`.
You can see how a simple pattern can decrease the entropy of the produced word stream.

Patterns come with a cost, though - after each occurence of a pattern word, we must emit some literals for the decoder to use. There's a plus side to it as well - supplementary literals can exist in a context of their own.
For example, the pattern `q*` will most likely have a supplementary `"u"`, which in a context of its own has an entropy of close to 0 bits. The example with `the*` has supplementary literals from the set of `{"\x20","m","n","r", ...}`. Encoding each of those in a context of their own saves us a lot of bits, compared to having to use the words `"the\x20"`,`"them"`,`"then"`,`"ther"` all together in the main context.

Another advantage to patterns is that there is practically no limit to what they can be and represent. They can be error correction codes, that get emitted when a limit of error is surpassed. They can be regex for emails, etc.

We may inject patterns that match different HTML tags, using context splitting for stuff like enwik8, enwik9:  
A pattern like `<(A+)>.</(A+)>` can cover a lot of text and split contexts into 2 - html tags (inbetween the `<` and `>`) and regular words / main context for the stuff between the tags. Some tags rarely have tags inside them and we may switch to that context, with a conditional on the html tag - if we have a `<p>` tag, it's likely there aren't any tags inside, but only words, which allows for a better tailored dictionary in this new context.

To be honest patterns are a bit of a cheat, as they're more context splitting orientated than dictionary related, but it is a fact no LZ coder can use non-pre-defined patterns.

### 5. Generating patterns

Obviously we can't generate all possible regex patterns at runtime, instead we can limit ourselves to a set of patterns and rules and explicitly add some useful regex's directly in the encoder (those don't need to be written to the stream if not used, as long as the decoder can recognise them in the dictionary).  
For each selected word (pre-ranking), we create all possible patterns that match that word, and add it to the list of words. We only need unique patterns. It is important patterns and words get ranked together, therefore the ranking function must be modified to accomodate for the information gain that supplementary literals/symbols create.

Here are some proposals for matching symbols in patterns: 
| Pattern symbol | What it matches |
| -------------- | --------------- |
| `*` | any character |
| `*+` or `.` | any amount of characters |
| `A` through `Z`, ie. uppercase letters | any repeating unique characters in a word |
|||

_Examples_:  
`the*` matches `them, the\x20, then, there, another, bother, father, mother, they, their, northern, prometheus`  
`.` matches all of `aaaa, abcd, a\x20long\x20word, t, m, tmmtmtmtmtm`  
`*AAi` matches `getting, tossing, possing, begging, shagging, tagging, snapping`, in fact here we might use `.AAing` better.
better?  
`AAe` matches `better, attention, acquitted, latter, ladder, lottery, logged, pegged, pattern, embedded`, here `AAed` is also very common. It is expected that BWD learns such lexical features as prefixes and postfixes.  
Here are some non-english examples too:  
`*о*а` matches `вода, кола, пола, трола, троша, пода, мола, пола, корона, трона, бона`  

## Areas of work and research

a) Lagrangian optimizations for ordering  
b) Context splitting  
c) Independent symbols  
d) Independent words

### 1b. Context splitting

Patterns are the very basic example of context splitting, and where the idea came from.

Note, context splitting is not a BWD-bound feature, like patterns.

Context has a different meaning in entropy coders and context mixers, because they process the data linearly. We'll define context as a new data stream that is embedded in the original. The idea is to emit additional context switching symbols inbetween contexts and switch all context releated feature for encoding the next region of data (like dictionaries, models, markov chains, whole data compression algorithms as well). This is kinda being done for archives, where images are compressed as a different context than text, but nothing is done dynamically yet.
Dynamic Huffman coders and some fancier LZ coders also employ this idea partially by reseting the trie or dictionary when their coding gets over a certain treshold over the optimal entropy. Sometimes they even preserve a part of the previous context for faster drop in entropy. But they never come back to a previous context or switch multiple times in a short amount of time.

We'll let the coder decide what contexts need to be used and leave it operate on those. This is pretty hard, but it will entropy at each context with minimal gain, for a good enough context splitting algorithm.

My initial thoughts are to create a sliding minimax window of sorts, with a size that is yet to be determined or calculatable. We'll move this window along the stream/block and we'll do the following at each position:  
Select all the symbols in this region (that the window covers). Color all the symbols in the stream that exist in this set. Everything that we've colored (including the window itself) will be our first context. The window is called a minimax one, because it is the minimal size of the biggest context region that will be colored. Next we recolor some stuff back / uncolor it, if you will. This is done to ensure only high-quality regions will be colored and can be done by looking at the amount of words that are overlap between colored regions. For example region1 contains 4 occurences of "the", while our window contains 0, we will give this region a lower weight.
We select the best regions and calculate information loss. For each position of the sliding window we know the best (computable this way) configuration of context splitting and the respective losses. We can now select the best one (with the biggest alleged information loss) and repeat the process if we want to create more contexts (2+).

### 1c. Independent symbols

I'll be brief with those, as I'm not sure they are very applicable.

We split the alphabet of symbols \\(A\\) into two disjoined stes of symbols:  
\\(\large S_1 \cup S_2 = A, S_1 \cap S_2 = \emptyset\\)  
All the words generated from those sets will be independent and can be ranked/ordered independently of the others in the final ranking/ordering.

### 1d. Independent words

Since disjoined sets produce a lot of symbols and not enough independ words, we can try graph all prefixes and postfixes of words and find independent word pairs or groups. The end goal is again to simplify ordering/ranking. Although it's not actually a simplification in the bigger picture, when ranking/ordering we have less restrictions and can use better optimization algorithms to find an optimal ordering.