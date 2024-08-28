g = "private uint[] generalcode = {"

with open("fullgecko.txt") as f:
    for x in f.readlines():
        for y in x.split():
            g += "0x" + y + ","
g += "};"
print(g)