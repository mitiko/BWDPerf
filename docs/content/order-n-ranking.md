# Order-n entropy ranking

We discussed entropy ranking [here](https://mitiko.github.io/BWDPerf/ranking) and it's advantages over naive ranking [here](https://mitiko.github.io/BWDPerf/ranking-comparison).

Entropy ranking calculates the change in compressed file size for a dictionary update $W \rightarrow W'$.
We can't afford to compress the file with each dictionary and check the size, so we estimate it with entropy instead.  
This is problematic for two reasons:

1) State of the art compressors use higher order models and make more accurate predictions about the symbol distribution, thus surpassing entropy.
2) State of the art compressors don't use a static probability distribution. Predictions are online, they evolve as we read more data.

We can fix 1) by using a higher order entropy estimation:
$$
H_n(X) = - \sum_{x \in X} \sum_{c \in C_{T,n}(X)} p(c)p(x|c) \log(p(x|c))
$$
where $C_{T, n}(X)$ are the possible order-n contexts of $X$ in a text $T$.

The approach to coming up with this is simple.  
We want to approximate the information content of a character by it's context.
The information of a character is $\log(p(x|c))$.
We have to multiply that by how often the string $cx$ occurs in the text - this is $p(cx)=p(c)p(x|c)$.

Notice that the empirical order-k entropy is a much more inacurate estimation of the compressed file size.
For example, the empirical $H_5$ of 200MB of random data is about $0.006$, while in practice we can't compress random data even nearly so well.
The difference with my approach is the added $p(c)$ term.

Addressing 2) is harder.
We can't afford to compute the model predictions for every possible dictionary update $W \rightarrow W'$ throughout the file.
It becomes a $O(n^3)$ computation.
With dynamic programming we can probably get that to roughly $O(n^2)$ but that's still too slow.
Instead we rely on the following hypothesis.

# Dictionary hypothesis

For big enough files the order-n entropy ranking produces dictionaries that are just as good as ranking words by completing the compression chain using the new dictionary, when the model is a mix of order-n submodels.

This is because at some point in the text the prediction about the probability distribution of the next symbol is assumed to be the same as the already observed data, while the entropy measure will calculate the information content of the  next character as a global static probability.

# Calculating the change in entropy

The equation for the change in symbol count remains the same as in order-0 entropy ranking but we need to update the way we calculate change of entropy.

Calculating the change in entropy is intractable here.
A word replacement induces a change in the proability of contexts, which in turn induces a change in the probability of posteriors dependent on that context.
This depth of 2 introduces many complications and is highly error-prone.

Instead we calculate the new entropy and the change in entropy is the difference.

The other change is that we no longer can use only the count of a word, we need all locations of the matches.