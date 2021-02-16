using System;
using System.Collections.Generic;
using System.Linq;

namespace BWDPerf.Tools
{
    public class SuffixArray
    {
        public int[] SA { get; }

        public SuffixArray(ReadOnlyMemory<byte> data)
        {
            // Prefix doubling - taking O(n log n) time
            // An optimization can be done to calculate a partial suffix array in O(n log m) where m is the biggest string we'll be searching by
            int n = data.Length;
            var s = data.Span;
            const int alphabet = 256;
            var p = new int[n]; // Backwards sorted positions of equivalence
            var c = new int[n]; // Equivalence classes
            var cnt = new int[Math.Max(n, alphabet)];

            // Sort the substrings of length 1 using count sort
            // Count the occurences
            for (int i = 0; i < n; i++)
                cnt[s[i]]++;
            // Change the array to contain the ending positions in the sorted array
            for (int i = 1; i < alphabet; i++)
                cnt[i] += cnt[i-1];
            // Save sorted indices of elements in reverse order
            for (int i = 0; i < n; i++)
                p[--cnt[s[i]]] = i;
            // Store the equivalence class of each element
            c[p[0]] = 0;
            int classes = 1;
            for (int i = 1; i < n; i++)
            {
                if (s[p[i]] != s[p[i-1]])
                    classes++;
                c[p[i]] = classes - 1;
            }
            // We don't really need the sorted characters, the equivalence classes will give us more information forward
            // So we don't complete the sort, just save the equivalence classes

            // Sort the suffixes of lengths 2, 4, 8, 16, 32... by using the information of the already sorted halves
            // Same as the positions and equivalence classes but for the next iteration
            var pn = new int[n];
            var cn = new int[n];
            for (int h = 0; (1 << h) < n; ++h)
            {
                // Calculate the positions array
                for (int i = 0; i < n; i++)
                {
                    // The position of the second half of the i-th substring is i - 2^(k-1)
                    pn[i] = p[i] - (1 << h);
                    if (pn[i] < 0)
                        pn[i] += n;
                }
                // Reset count
                for (int i = 0; i < classes; i++)
                    cnt[i] = 0;
                // Count the classes
                for (int i = 0; i < n; i++)
                    cnt[c[pn[i]]]++;
                // Calculate the ending postions in the sorted array
                for (int i = 1; i < classes; i++)
                    cnt[i] += cnt[i-1];
                // Calculate the positions
                for (int i = n-1; i >= 0; i--)
                    p[--cnt[c[pn[i]]]] = pn[i];
                cn[p[0]] = 0;
                classes = 1;
                for (int i = 1; i < n; i++) {
                    // Check if the substrings are of the same class by checking both halves
                    var cur = (c[p[i]], c[(p[i] + (1 << h)) % n]);
                    var prev = (c[p[i-1]], c[(p[i-1] + (1 << h)) % n]);
                    if (cur != prev)
                        ++classes;
                    cn[p[i]] = classes - 1;
                }
                // Update the classes
                c = cn;
            }

            this.SA = p;
        }

        public void Print()
        {
            Console.WriteLine("Suffix array:");
            foreach (var x in this.SA)
                Console.WriteLine(x);
        }

        public int[] Search(ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> word)
        {
            // Binary search on the sorted suffix array takes O(m log n) worst case for word size m
            // FM-index stores BWT and can search in O(m) with a constant rank function using a wavelet tree
            int low = 0;
            int high = data.Length - 1;
            int match = -1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                for (int i = 0; i < word.Length; i++)
                {
                    if (this.SA[mid] + i >= data.Length) { low = mid + 1; break; }
                    var sym = data.Span[this.SA[mid] + i];
                    if (sym == word.Span[i])
                    {
                        if (i == word.Length - 1)
                            match = mid;
                        continue;
                    }

                    if (word.Span[i] > sym)
                        low = mid + 1;
                    if (word.Span[i] < sym)
                        high = mid - 1;
                    break;
                }
                if (match != -1)
                    break;
            }

            if (match == -1)
                return new int[0];

            int first = match; // First match inclusive
            int last = match;  // Last  match exclusive
            while (IsMatch(first - 1))
                first--;
            while (IsMatch(last))
                last++;

            var result = this.SA[first..last];
            Array.Sort(result);
            // Remove the last few positions which actually terminate before a whole word is matched because of the cyclic shifting
            last = result.Length;
            for (int i = result.Length - 1; i >= 0 ; i--)
                if (result[i] + word.Length - 1 >= data.Length) last--;
            return result[..last];

            bool IsMatch(int index)
            {
                if (index < 0) return false;
                if (index >= this.SA.Length) return false;
                var pos = this.SA[index];
                // This is actually not a match but we'll pretend it is and remove it from the result later
                if (pos + word.Length - 1 >= data.Length) return true;
                for (int i = 0; i < word.Length; i++)
                    if (data.Span[pos + i] != word.Span[i]) return false;
                return true;
            }
        }
    }
}