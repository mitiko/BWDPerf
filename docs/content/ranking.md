# Ranking

Let's provide a better ranking algorithm.

## What we have for now

Conceptually, the rank should represent change in information.
The idea is that after the transform, we should have a more compressible stream.

One way to do that is to use the word's count and length.
At every location the word matches, we'll replace with a single token, so the savings at each location would be $(length - 1)$ characters.
Since we're doing this at every location, and we also need to store the word in the dictionary, we can count the number of repeated occurences to be $(count - 1)$.
Thus, we can calculate a saving of ${count * (length - 1) - length} = {(count - 1) * (length - 1) + 1}$ total characters.  
And since the ranking function is the same for every word, we can shift that by 1 to get:
$$
r(w) = (w.Count - 1) * (w.Length - 1)
$$

## Improving

The current algorithm is very good at selecting words that will minimize the symbol count. There're 2 problems though:
1) It's greedy
2) It doesn't represent the savings in entropy, only symbol count

The first problem is solvable, but the second one is more relevant, I think.  
Basically, a word that is shorter and not very common might be better than a long common word at times, because it reduces the overall entropy. The measure that we're looking for is not savings in the post-transform symbol count, it's savings in the entropy comressed size.

# Entropy ranking

The easiest way to think about the problem is optimizing our alphabet / dictionary.  
In the beggining, our dictionary will be just the alphabet:
$$
\mathbb{A} = \{ x_i \}
$$
$$
W_0 = \mathbb{A}
$$
We'll do incremental updates to this dictionary, while beneficial.
We'll calculate the savings when adding a new word.
$$
W_{i+1} = W_i \cup w
$$
$$
w = \overline{x_{\alpha_0}x_{\alpha_1}x_{\alpha_2}...x_{\alpha_l}}
$$
$$
x_{\alpha_i} \in \mathbb{A}
$$
In practice, since some set of words may fully cover some symbols, we're pretending the dictionary contains the alphabet until no savings can be measured. Then we check which symbols haven't been covered by a word and add them to the dicionary as individual words of length 1.

Let $T$ be the text we're going to compress.
Defined as a set of symbols of the alphabet ${T = \{ t | t \in \mathbb{A} \}}$. $T$ doesn't have to be text, it can be any type of file, as long as all symbols in it belong to a fixed alphabet.

Let $s=|T|$ be the size of the text.  
We know that the entropy of $T$ is:
$$
H(\mathbb{A}) = - \sum_{x_i \in \mathbb {A}} p(x_i) \log(p(x_i))
$$
where $p(x_i)$ is the probability of finding $x_i$ at any location in $T$.

We can now approximate the size of the encoded file with:
$$
E = s \times H(\mathbb{A})
$$
This is by definition of entropy - the average bits per symbol needed to encode the symbol.

For a chosen dictionary $W$ we can define the same functions.  
After parsing, entropy becomes:
$$
H(W) = - \sum_{w_i \in W} p(w_i) \log(p(w_i))
$$
For the encoded size, we need a little bit of trickery to calculate the count of symbols after the transform.

Each word occurs $s \times p(w)$ times. Then the total count of symbols comes up to:
$$
C(W) = \sum_{w_i \in W} s \times p(w_i) = s \times \sum_{w_i \in W} p(w_i)
$$

Let us aknowledge that $C(\mathbb{A}) = s$. This is because ${\sum p(x_i) = 1}$.
We can also make the observation that for any dictionary ${C(W) \le s}$.

The encoded size, after using a dictionary comes up to:
$$
E_W = C(W) \times H(W)
$$
And for $W_0 = \mathbb{A}$, so $E = E_{W_0}$.

We want to maximize the change in the encoded size:
$$
y = \max_{W}(E - E_W)
$$
To make this less computationally expensive we do a greedy approximation:
$$
y \approx \sum_{i} \max_{w} (E_{W_{i-1}} - E_{W_{i}})
$$
We'll terminate this process, when ${E_{W_{i-1}} - E_{W_{i}} \le 0}$.

Let $\Delta E(w) = E_{W_{i-1}} - E_{W_{i}}$.  
We can define our ranking function to be $r(w) = \Delta E(w)$.

Let's also define the change in symbol count and entropy:  
$\Delta C(w) = C(W_{i-1}) - C(W_i)$

$\Delta H(w) = H(W_{i-1}) - H(W_i)$

Then it follows that:
$$
\Delta E(w) = C(W_{i-1}) \times H(W_{i-1}) - C(W_i) \times H(W_i)
$$
We can rearange the equations for change in count and entropy:  
$C(W_{i-1}) = \Delta C(w) + C(W_i)$
$H(W_{i-1}) = \Delta H(w) + H(W_i)$

And now, we apply the discrete derivative equivalent of the product rule.
$$
\Delta E(w) = (\Delta C(w) + C(W_i)) \times (\Delta H(w) + H(W_i)) - C(W_i) H(W_i)
$$
$$
\Delta E(w) = \Delta C(w)\Delta H(w) + \Delta C(w) H(w) + C(W_i) \Delta H(w) + C(W_i) H(W_i) - C(W_i) H(W_i)
$$
$E_{W_i}$ cancels out.
$$
\Delta E(w) = \Delta C(w) H(w) + C(W_i) \Delta H(w) + \Delta C(w)\Delta H(w)
$$

This seems pretty complicated but we only need to calculate $\Delta C$ and $\Delta H$.

Then the ranking looks something like this:

```
def Rank(Word w):
    dC = DeltaC(w)
    dH = DeltaH(w)
    return C*dH + H*dC + dC*dH

def ChooseWord(Word w):
    dC = DeltaC(w)
    dH = DeltaH(w)
    C = C - dC
    H = H - dH
```

Let's derive the change in symbol count.
$$
\frac{\Delta C}{s} = \sum_{w_i \in W} p(w_i) - \sum_{w_i \in W'} p(w_i)
$$
This is not $0$ because $W' \ne W$ and the probabilities don't match.
We can re-write the new probabilities as a conditional statement with prior - the new word being in the dictionary.
$$
\frac{C(W_i)}{s} = \sum_{w_j \in W_{i-1}} p(w_j | w \in W_i) + p(w)
$$
Now, this conditional probability can be calculated as:

${p(w_j | w \in W') = p(w_j) - p(w)}$ when ${w_j \in w}$

${p(w_j | w \in W') = p(w_j)}$ when $w_j \notin w$.

$w_j \in w$ is interperted as $w_j$ being a substring of $w$.  
In a rigorous notation, that is:

For ${w = \overline{x_{\alpha_0}x_{\alpha_1}x_{\alpha_2}...x_{\alpha_l}}}$ and ${w_j = \overline{x_{\beta_0}x_{\beta_1}x_{\beta_2}...x_{\beta_k}}}$, with ${x_{\alpha_i}, x_{\beta_i} \in \mathbb{A}}$:  
$$
w_j \in w \Leftrightarrow \exists i, j:
\overline{x_{\alpha_i}x_{\alpha_{i+1}}...x_{\alpha_j}} = \overline{x_{\beta_0}x_{\beta_1}x_{\beta_2}...x_{\beta_k}}
$$
or in a more programatic way:
$$
w_j \in w \Leftrightarrow \exists i, j=i+k:
\forall s \in [0, k]: x_{\beta_s} = x_{\alpha_{i+s}}
$$
It is important to note that the conditional probability can be calculated like that, only when the new word is chosen to be aligned with the parsing of the previous dictionary!
$$
\frac{C(W_i)}{s} = \sum_{w_j \in W_{i-1}, w_j \notin w} p(w_j) + \sum_{w_j \in W_{i-1}, w_j \in w} \Big( p(w_j) - p(w) \Big)+ p(w)
$$
The delta then becomes:
$$
\frac{\Delta C}{s} = \sum_{w_i \in W} p(w_i) - \sum_{w_i \in W, w_i \notin w} p(w_i)
- \sum_{w_i \in W, w_i \in w} \Big( p(w_i) - p(w) \Big) - p(w)
$$
Knowing $\{w_i | w_i \in W, w_i \notin w \} \in W$, we can cancel a part of the first sum with the second sum.
$$
\frac{\Delta C}{s} = \sum_{w_i \in W, w_i \in W} p(w_i)
- \sum_{w_i \in W, w_i \in w} \Big( p(w_i) - p(w) \Big )
- p(w)
$$
Now we can split the second sum.
$$
\frac{\Delta C}{s} = \sum_{w_i \in W, w_i \in W} p(w_i)
- \sum_{w_i \in W, w_i \in w} p(w_i)
+ \sum_{w_i \in W, w_i \in w} p(w)
- p(w)
$$
We can cancel the first 2 sums again.
$$
\frac{\Delta C}{s} = \sum_{w_i \in W, w_i \in w} p(w)
- p(w)
$$
This can be re-written as:
$$
\frac{\Delta C}{s} = p(w) \times \Big | \Big \{ w_i \in W | w_i \in w \Big \} \Big | - p(w)
$$


Let's derive the change in entropy now.

$$
\Delta H = - \sum_{w_i \in W} p(w_i) \log(p(w_i)) + \sum_{w_i \in W'} p(w_i) \log(p(w_i))
$$
We have to use the same trick with the conditional probabilities:
$$
- H(W_i) = \sum_{w_j \in W_{i-1}} p(w_j | w \in W_i) \log \Big(p(w_j | w \in W_i) \Big) + p(w) \log(p(w))
$$
$$
- H(W_i) = \sum_{w_j \in W_{i-1}, w_j \notin w} p(w_j) \log(p(w_j))
+ \sum_{w_j \in W_{i-1}, w_j \in w} \Big(p(w_j) - p(w)\Big) \log \Big(p(w_j) - p(w) \Big)
+ p(w) \log(p(w))
$$
Now we can substitute that in the delta.
$$
\Delta H = - \sum_{w_i \in W} p(w_i) \log(p(w_i)) + \sum_{w_i \in W, w_j \notin w} p(w_i) \log(p(w_i))
+ \sum_{w_i \in W, w_i \in w} \Big(p(w_i) - p(w)\Big) \log \Big(p(w_i) - p(w) \Big)
+ p(w) \log(p(w))
$$
We can cancel out the first 2 sums.
$$
\Delta H = - \sum_{w_i \in W, w_i \in w} p(w_i) \log(p(w_i))
+ \sum_{w_i \in W, w_i \in w} \Big(p(w_i) - p(w)\Big) \log \Big(p(w_i) - p(w) \Big)
+ p(w) \log(p(w))
$$
Now we can combine the 2 sums.
$$
\Delta H = \sum_{w_i \in W, w_i \in w} \Bigg( \Big(p(w_i) - p(w)\Big) \log \Big(p(w_i) - p(w) \Big) - p(w_i) \log(p(w_i)) \Bigg)
+ p(w) \log(p(w))
$$

And we're done! It's a bit ugly but this is a delta, you can't ask for more.

This is all we needed.

The last thing we should do is to adjust the ranking function to take into account the dictionary overhead that a word generates. It's hard to approximate the symbol probability within the dictionary, so we'll rank words as if no compression will be applied to the dictionary.

An easy function for that is:
```
def dictOverhead(Word w) => - 8 * (w.Length + 1)
```
It needs to be in bits, because the change in compressed size is measured in bits, so that is why we multiply by 8. This one is when we output a byte to indicate word length and another `w.Length` bytes for each character in the word.
Then we just add this to the rank.

The ranking is as follows:
```
Choose the word with highest rank.
Upate the count and entropy.
Repeat until there're no more words or adding a word to the dictionary is inefficient.
Go through symbols that haven't been covered.
    Add symbol to the dictionary as a word with length 1.
```

## Implementation details

There're a couple of things that change in the implementation.

We're taking $\frac{C(W)}{s}$ instead. The idea is to save an extra multiplication. We only have to adjust the dictionary overhead, everything else is divisible by $s$.

The other change that I'm doing for now is changing the conditional probability:

${p(w_j | w \in W') = p(w_j) - p(w)}$ when ${w_j \in w}$ and $|w_j| = 1$

${p(w_j | w \in W') = p(w_j)}$ otherwise.

This reduces the accuracy but we would have to do $O(n^2)$ operations to check for matches for $n$ - the count of words. Even if we were clever and used the suffix array to do the matching, it would be too slow.  
To implement it, I basically have to re-write how the suffix array represents symbols. Also, we should do something more clever for the dictionary encoding, since words will overlap.
Anyway, not impossible, and an $O(n \log n)$ solution probably exists.

## Next steps

The cool thing about this entropy estimation is that the encoder and decoder don't have to use the same probabilities. The closer the dictionary is tuned to the real weights, the better the encoder should be. But if the encoder is way off from appropriate predictions, it might be better to tune the dictionary to how the decoder predicts.

So, yeah, order-n models with a mixer will decide better if a word should be added to the dictionary. There's also an inbetween, where we use the same structure for choosing words to add to the dictionary, but we do it on-line and not on the whole block. Then we can model the top couple of words and predict multiple symbols at once. This is quite advanced and I think adding some patterns may aid compression more in the short term.

To make it all go faster, we can do some approximations on the probabilities. Maybe even skip ranking words if we're certain they're not going to outrank the current best, by using some strong-ish inequality. The other thing to do is implement lookup tables - for the log, for plogp and such.

After patterns, the idea that is most likely to produce better results is lookahead.
This is trying to make the ranking less greedy.
We look ahead in time and check the top 7-8 ranked words. For a depth of 4-5 we can get a better word sequence. This is also a bit hard, and I'm planing on optimizing some of the data structures used already, before going full in, because it's not certain that lookahead will produce better words.