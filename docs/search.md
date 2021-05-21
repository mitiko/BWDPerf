<head>
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/katex@0.10.2/dist/katex.min.css" integrity="sha384-yFRtMMDnQtDRO8rLpMIKrtPCD5jdktao2TV19YiZYWMDkUR5GQZR/NOVTdquEx1j" crossorigin="anonymous">
<script defer src="https://cdn.jsdelivr.net/npm/katex@0.10.2/dist/katex.min.js" integrity="sha384-9Nhn55MVVN0/4OFx7EE5kpFBPsEMZxKTCnA+4fqDmg12eCTqGi6+BB2LjY8brQxJ" crossorigin="anonymous"></script>
<script defer src="https://cdn.jsdelivr.net/npm/katex@0.10.2/dist/contrib/auto-render.min.js" integrity="sha384-kWPLUVMOks5AQFrykwIup5lo0m3iMkkHrD0uJ4H5cjeGihAutqP0yW0J6dpFiVkI" crossorigin="anonymous" onload="renderMathInElement(document.body);"></script>
</head>

# BWDPerf - Search

The BWD algorithm requires using a dictionary of very good words.  
Choosing such a dictonary efficiently can only be done by ranking the words.  
The best ranking functions currently, very intuitively, use the number of occurences of a word.  
But to find this count of word in the buffer, a search must be performed.

## The problem

Counting words in a string seems like an extremely easy task. We humans only need a glimpse of a page of text to skim through all the paragraphs and check if a word exists. Counting makes it harder, we'll leave that job to the computers.

Counting is a very unanticipated problem and it arises when we encounter long sections of a string containing the same character like `aaaaaabbbaaaaabcdeaaaaaa`. Because substrings like `aaa` exist a lot, it is hard to count how many times exactly the substring can fit into the text.

Storing the result is also not a very easy job.
The intuitive key-value pair, dictionary or hashtable if you prefer will store the entire data in its key section about more than \\(m^2\\) times, where \\(m\\) is the max word size.  
This consumes way more memory than is actually necessary.
Then finding locations using a hash fucntion further slows the process.
The biggest setback of this method, though, is having to collect the counts after each choosing of a word for the dictionary.  
This happens because the data itself changes and gets split.
When choosing words one by one this is unavoidable, but...

There's a hack to record the changes in the text and handle the counts at only the places changed.
The problem with it is that everything becomes too messy.
You have to recount the words in the changed regions and you have to keep track of splits that happened close enough (which is pretty regular too) that words can span between them.
It becomes too much work for a solution already flawed.

Instead of fixing flaws, we can reinvent the counting process:

## Counting

As we established, recounting is unavoidable, we can only work around it to support the concept.
The idea of recording the delta/changes in the original text is not too bad, we just have to execute it better.
Let's create a structure that keeps track of matching words.

We'll call it `wordRef` for word references.
We will make each location in the text point to the first location where the current word was observed.
This way all words matching some `abc` will point to the first location of `abc`.
Now when we count `abc` we'll go through `wordRef` and count the locations where the value is also the initial location of `abc`.

The cool part is that these locations are absolute to the initial text/buffer.
If we were to delete part of the text that contains the first occurence of a word, it wouldn't matter, because all other locations point to it.
It's basically also how BWD works - we delete most of the text by deleting words and replacing them with referential tokens that point to the dictionary where the real value of the word is stored.

This works great and we've lowered memory consumption by a factor of more than 10 but now finding matches is
\\(O(n^2)\\) instead of the previous \\(O(nm)\\) for all hash function invocations.
This is extremely slow and we've traded speed and accuracy for memory.

Let's get it back.
I'm currently testing certain string searching algorithms that perform better than the naive approach.
As you can see there are plenty: Knuth-Morris-Pratt, Boyer-Moore, Raita, Aho-Corasick, Commentz-Walter, Rabin-Karp, Crochemore-Perrin.

Ultimately, the FM-index will be our saviour. It basically relies on the suffix array, which is the backbone of the Burrows-Wheeler Transform.
This implies that we can also drop back to using BWT with MTF and an entropy coder, instead of forcing words in a dictionary and compress near-incompressible text with less overhead.
