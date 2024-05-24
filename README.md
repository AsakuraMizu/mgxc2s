# mgxc2s - Margrete2S modified by Asakura Mizu

Source code: https://github.com/AsakuraMizu/mgxc2s

Original source code from [Margrete2S / Margrithm](https://margrithm.girlsband.party/)

## Modification from original version

- Remove curved slide generating
- Auto use ex-hold/ex-slide
- Fix incorrect ex-hold effect
- Fix incorrect extap merging: if a normal extap (with air or something) and a marker extap for hold/slide are in the same place, the normal extap will not be removed
- Fix metre: in c2s, numerator should be placed in front of denominator (thanks [Chunithm-Research](https://github.com/Suprnova/Chunithm-Research/blob/main/Charting.md))
- No `note-at-start`: time is preserved as-is and start from 0 (but you should not put a note at start)
- Fix incorrect air height
- Parsing extap effect (notice that margrete/umiguri has one more effect "OI (Outside to Inside)" than chunithm; this will be converted into the same effect as "IO")
- **(Update) Support note speed change**  
  **Notice**: Due to limitation of the game, you can not set note speed to `0` even if it works in UMIGURI. Please use something like `0.00001`

## TODO

- ~~Inversed air (hint: inversed upside air has color of PNK (and the following air-hold/air-slide if exists) and inversed downside air has color of GRN)~~
  Implemention of this feature has been suspended. It turns out that "air"s (including air-slide) can have many colors as crush line, and the direction of the initial air of air-slide is not restricted as well. So it's Margrete/UMIGURI's fault to limit this functionality, and will possibly be fixed in UMIGURI NEXT.
