# Font dei PDF

| File | Font | Licenza | Uso |
|---|---|---|---|
| `DejaVuSansPdf.ttf`, `DejaVuSansPdf-Bold.ttf` | DejaVu Sans 2.37, copia modificata | Bitstream Vera + DejaVu (libera, ridistribuibile; le copie modificate non devono chiamarsi «Bitstream» o «Vera») | latino, greco, cirillico, arabo (forme di presentazione) |
| `DroidSansFallbackFull.ttf` | Droid Sans Fallback (Android) | Apache 2.0 | ideogrammi cinesi, giapponesi, coreani |

**Perché una copia modificata di DejaVu Sans.** Molti glifi delle forme arabe sono composti annidati (per esempio la
qaf iniziale = «qaf senza punti iniziale», a sua volta composto, + «due punti»): PDFsharp, quando incorpora solo i glifi
usati, perde i componenti del secondo livello e la lettera esce senza corpo. Nella copia tutti i composti sono scomposti
in contorni semplici; il nome della famiglia è «DejaVu Sans PDF».

Rigenerarla (fontTools, dai file originali `DejaVuSans.ttf` e `DejaVuSans-Bold.ttf`):

```python
from fontTools.ttLib import TTFont
from fontTools.pens.recordingPen import DecomposingRecordingPen
from fontTools.pens.ttGlyphPen import TTGlyphPen

for src, dst in [("DejaVuSans.ttf", "DejaVuSansPdf.ttf"), ("DejaVuSans-Bold.ttf", "DejaVuSansPdf-Bold.ttf")]:
    font = TTFont(src)
    glyf, glyphs = font["glyf"], font.getGlyphSet()
    for name in font.getGlyphOrder():
        if glyf[name].isComposite():
            rec = DecomposingRecordingPen(glyphs)
            glyphs[name].draw(rec)
            pen = TTGlyphPen(None)
            rec.replay(pen)
            glyf[name] = pen.glyph()
    for record in font["name"].names:
        text = record.toUnicode()
        record.string = text.replace("DejaVu Sans", "DejaVu Sans PDF").replace("DejaVuSans", "DejaVuSansPDF")
    font.save(dst)
```
