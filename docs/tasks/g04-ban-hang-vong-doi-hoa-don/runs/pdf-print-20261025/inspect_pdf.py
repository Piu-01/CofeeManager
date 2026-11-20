import json
import re
import sys
from pathlib import Path
from pypdf import PdfReader

source = Path(sys.argv[1])
output = Path(sys.argv[2])
reader = PdfReader(source)
pages = [page.extract_text() or "" for page in reader.pages]
text = "\n".join(pages)
normalized = re.sub(r"\s+", " ", text)
header = next((line.strip() for line in text.splitlines() if line.strip()), "")
checks = {
    "readable_pdf_with_pages": len(pages) > 0 and bool(text.strip()),
    "correct_invoice": "HD20250421" in normalized,
    "first_print_not_labeled_reprint": "IN LẠI" not in header,
    "payable_81000": bool(re.search(r"PHẢI TRẢ\s*:\s*81\.000\s*₫", normalized)),
    "cash_allocation_81000": bool(re.search(r"Tiền mặt\s*:\s*81\.000\s*₫", normalized)),
    "correct_product": "SP001" in normalized,
    "fixture_note_retained": "OO pagination fixture" in normalized,
    "no_unpersisted_tender_or_change": "100.000" not in normalized and "19.000" not in normalized,
}
result = {"source": str(source), "pages": len(pages), "header": header, "checks": checks,
          "passed": sum(checks.values()), "failed": sum(not value for value in checks.values()),
          "scope": "Text/content only; no visual QA or physical printer claim", "text": pages}
output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps({"pages": len(pages), "checks": checks}, ensure_ascii=False))
