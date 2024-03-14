# mgxc2s - Margrete2S modified by Asakura Mizu

Source code: https://github.com/AsakuraMizu/mgxc2s (sorry private repo for now, will turn public after i finish `ugc2s` mentioned in [Extra Info](#extra-info))

Original source code from [Margrete2S / Margrithm](https://margrithm.girlsband.party/)

## Modification from original version

- Remove curved slide generating
- Auto use ex-hold/ex-slide
- Fix incorrect ex-hold effect
- Fix incorrect extap merging: if a normal extap (with air or something) and a marker extap for hold/slide are in the same place, the normal extap will not be removed
- Fix metre: in c2s, numerator should be placed in front of denominator (thanks [Chunithm-Research](https://github.com/Suprnova/Chunithm-Research/blob/main/Charting.md))
- No `note-at-start`: time is preserved as-is and start from 0 (but you should not put a note at start)

## TODO

- Parsing extap effect (notice that margrete/umiguri has one more effect than chunithm)
- Inversed air (hint: inversed upside air has color of PNK (and the following air-hold/air-slide if exists) and inversed downside air has color of GRN)

## Extra Info

I made this modified version just because i have no time to completely rewrite it. In January or maybe later I will release `ugc2s` which is a complete rewrite in rust with more features.
