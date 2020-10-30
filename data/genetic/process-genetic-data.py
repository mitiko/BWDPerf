f = open("file.txt", "w")
g = open("genetic.txt", "r").read()
g = g.replace(" ", "")
g = g.replace("\n", "")
for i in range(10):
    g = g.replace(str(i), "")

f.write(g)