# Single layer probability mixer

- Goal: Mixes probability distribution from multiple models to form a better prediction
- Benchmark: Compressed size. (Order-4 mixed model should reach under 221,000 bytes)
- Learning type: offline

## Architecture

Version 1 is with no bias vector.  
Let N be the number of output symbols, in this case 256.  
Let M be number of models. For order-4 mix we have M = 5 (o0, o1, o2, o3, o4).  
Indexing: $0 \le j \le M-1$, and $0 \le i \le N-1$.  
Each model $p_j$ is given a weight $0 \le w_j \le 1$.  
Each model produces a probability dsitribution:
$$
p_j = [ p_{j0}, p_{j1}, p_{j2} ,..., p_{jN}]
$$
$$
\sum_{j=0}^{N} p_{ji} = 1
$$
Models are weighted and the result is normalized to achieve a single probability distribution:
$$
P = norm(\sum\limits_{j=0}^{M-1} w_j p_j)
$$
$$
\text{If } P = [P_0, P_1, P_2,..., P_N] \text{, then}
$$
$$
P_i = \frac{\sum\limits_{j=0}^{M-1} w_j p_{ji}}{s} \text{, where } s \text{ is a normalization factor}
$$
$$
\text{We want } \sum_{i=0}^{N-1} P_i = \sum_{i=0}^{N-1} \frac{\sum\limits_{j=0}^{M-1} w_j p_{ji}}{s} := 1
$$
$$
\text{Since } s \text{ is a constant, } s = \sum_{i=0}^{N-1}\sum_{j=0}^{M-1} w_j p_{ji}
$$
$$
s = \sum_{j=0}^{M-1}\sum_{i=0}^{N-1} w_j p_{ji} =
\sum_{j=0}^{M-1} w_j ( \sum_{i=0}^{N-1} p_{ji} ) =
\sum_{j=0}^{M-1} w_j
$$
$$
P_i = \frac{\sum\limits_{j=0}^{M-1} w_j p_{ji}}{\sum\limits_{j=0}^{M-1} w_j}
$$

## Backpropagation

With a loss function $L$ set, we can backpropagate the error and find the gradient to perform gradient descent, trying to minimize the loss.

With compression the loss for entropy coding is defined by the arithmetic coder / RANS coder. With really long inputs it appoaches $- \log_2 x_i$.

$$L = -\log_2 P_i$$

Now we just need to find the gradient with respect to the weights. Using the chain rule:

$$
\frac{\partial L}{\partial w_j} = \frac{\partial L}{\partial P_i} \frac{\partial P_i}{\partial w_j}
$$

The loss function is easily differentiable.

$$
\frac{\partial L}{\partial P_i} = - \frac{1}{P_i \ln 2}
$$

This is with respect to the probability of the encountered symbol from the mixed probability distribution.

$$
\frac{\partial P_i}{\partial w_j} =
\frac{
    \partial
        \frac
            {\sum\limits_{k=0}^{M-1} w_k p_{ki}}
            {\sum\limits_{k=0}^{M-1} w_k}
    }{\partial w_j}
$$

We can use the quotient rule:

$$
\frac{\partial P_i}{\partial w_j} =
\frac{p_{ji} s - 1 (\sum\limits_{k=0}^{M-1} w_k p_{ki})}{s^2} =
\frac{p_{ji} - P_i}{s}
$$

And we're done. Now we can zero out the gradient and make a small step.

### Sigmoid problem

In practice, to enforce $0 \le w_j \le 1$ we hide the weight under a sigmoid.

$$
\sigma(x) = \frac{1}{1+e^{-x}}
$$

Then we have a real weight $r_j$ and $w_j = \sigma(r_j)$.

So we need to propagate under that as well. Luckily, the sigmoid function is very common and has it's derivative derived:

$$
\frac{\partial L}{\partial r_j} = \frac{\partial L}{\partial w_j} \frac{\partial w_j}{\partial r_j} =
\frac{\partial L}{\partial w_j} \sigma(r_j)(1-\sigma(r_j))
$$

All set now.

After trying a lot, I concluded the model does not benefit from a bias, at least not in the form of a non normalized vector with each weight bounded by -1 and 1.
In other words - a bias vector just emulates the order0 model and just further complicates the code and the calculations, while giving little to no gain.
