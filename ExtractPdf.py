from pdfminer.high_level import extract_text
import sys, pathlib
p = pathlib.Path("Assessment.pdf")
if not p.exists():
    sys.exit('Assessment.pdf not found')
text = extract_text(str(p))
path = pathlib.Path("Assessment.extracted.txt")
path.write_text(text, encoding='utf-8')
print(f'Extracted to: {path.resolve()} (chars={len(text)})')
