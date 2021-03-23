# Comparison of entropy ranking and naive ranking

The two ranking functions we'll compare are:

$r_1(w) = (w.Length - 1) * (w.Count - 1)$

$r_2(w) = C * dH(w) + H * dC(w) + dC(w) * dH(w) - dict(w)$

# File size and run time

Testing on book1 (768'771) with max word size set to 32:

| Metric                 | Entropy ranking | Naive ranking   |
|------------------------|-----------------|-----------------|
| Time                   | 32:39 min       | 45:52 min       |
| Size estimation        | 365'182         | 393'222         |
| Size                   | 488'956         | 582'235         |
| Dictionary size        | 764 words       | 1185 words      |
| Dictionary length      | 2'495 bytes     | 3'565 bytes     |
| Stream size            | 389'165 symbols | 420'848 symbols |
| Ranking time           | 00:11.7 sec     | 00:03.1 sec     |
| Counting time          | 32:10 min       | 45:52 min       |
| Counting time per word | 00:02.52 sec    | 00:02.32 sec    |

_\* Size - words are assigned a static length code of log(dictSize)_  
_\* Size estimation - estimation after processing with an order0 entropy coder._  
_\* Ranking time - total time spent ranking._  
_\* Counting time - total time spent counting._  
_\* Counting time per word - average time spent counting per word in the dictionary._

### Conclusions

File size is reduced with and without an entropy coder, when using the new ranking.  
Run time is also reduced, but this is because of the near constant counting time per word in the dictionary in both cases multiplied by less words in the dictionary.  
The increase in time spent ranking is compared to the decrease in counting time.

Going forward, it seems an order-1 probability estimation won't slow ranking down too much.  
Optimizations must be made in the counting - i.e. re-counting may sometimes be slower than a full count, especially in the beginning.

# Quality of words

Let's check the first 20 words of both dictionaries:

| Id | Entropy ranking | Naive ranking |
|----|-----------------|---------------|
| 1  | `" the"`        | `" the"`      |
| 2  | `" and "`       | `"e "`        |
| 3  | `"ing"`         | `" a"`        |
| 4  | `" of"`         | `"in"`        |
| 5  | `"er"`          | `"d "`        |
| 6  | `" to "`        | `"er"`        |
| 7  | `" w"`          | `"t "`        |
| 8  | `"ou"`          | `"s "`        |
| 9  | `"Bathsheba"`   | `"th"`        |
| 10 | `" that"`       | `"ou"`        |
| 11 | `"ed"`          | `"on"`        |
| 12 | `" a"`          | `" s"`        |
| 13 | `"in"`          | `"en"`        |
| 14 | `" s"`          | `", "`        |
| 15 | `"th"`          | `"an"`        |
| 16 | `"on"`          | `"to"`        |
| 17 | `" h"`          | `"y "`        |
| 18 | `" be"`         | `"re"`        |
| 19 | `" --"`         | `" h"`        |
| 20 | `"re"`          | `"or"`        |

Entropy ranking ranks common prefixes and suffixes higher.

Here's on the length:

| Metric                             | Entropy ranking | Naive ranking |
|------------------------------------|-----------------|---------------|
| Average word size in top 20        | 3.1000          | 2.1000        |
| Average word size                  | 2.2604          | 2.0050        |
| Max word size                      | 27              | 11            |
| Character coverage of top 20 words | 33.96%          | 40.74%        |
| Symbol coverage of top 20 words    | 26.76%          | 35.32%        |

It seems that the more evenly distributed words are, the better compression is.
Evenly distributed words are caused by words that contain roughly the same information.