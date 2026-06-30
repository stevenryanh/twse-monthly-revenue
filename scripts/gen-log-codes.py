#!/usr/bin/env python3
"""從 TwseLogCodes.cs 產生 docs/LOG-CODES.md（單一真相來源，避免手寫漂移）。

借鑑 團隊 的做法：日誌代碼字典用「產生」而非手維護，CI 以 --check 防漂移。
用法：
  python3 scripts/gen-log-codes.py          # 產生/覆寫 docs/LOG-CODES.md
  python3 scripts/gen-log-codes.py --check   # 檔案與來源不一致則 exit 1（供 CI）
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "api/src/TwseRevenue.Application/Logging/TwseLogCodes.cs"
DOC = ROOT / "docs/LOG-CODES.md"

LEVELS = {"I": "Info", "W": "Warning", "E": "Error", "D": "Debug"}
LAYERS = {"1": "Api/Presentation", "3": "Application", "5": "Infrastructure", "6": "CrossCutting"}

# 比對：public const string NAME = "CODE"; // 描述
CONST_RE = re.compile(r'public\s+const\s+string\s+(\w+)\s*=\s*"([^"]+)"\s*;\s*//\s*(.+?)\s*$', re.MULTILINE)

HEADER = """# 日誌代碼字典（LOG Codes）

> ⚠️ 本檔由 `scripts/gen-log-codes.py` 從 `api/src/TwseRevenue.Application/Logging/TwseLogCodes.cs`
> 產生，**請勿手改**；改代碼或描述請改該 C# 檔後重跑產生器（CI 以 `--check` 把關，防漂移）。

每條日誌都以一個結構化代碼開頭（精神比照 團隊 的 `結構化日誌代碼`），便於分類、grep、跨服務一致。
**API caller / 維運人員**在日誌或錯誤回應看到代碼時，可在此查回正體中文意義。

代碼格式：`{Level}{Layer}{Seq}`
- **Level**：`I`=Info、`W`=Warning、`E`=Error、`D`=Debug
- **Layer**：`1`=Api/Presentation、`3`=Application、`5`=Infrastructure、`6`=CrossCutting
"""

FOOTER = """## 追蹤碼（CorrelationId / traceId）

每個請求由 `CorrelationIdMiddleware` 指派一組 `X-Correlation-ID`（沿用上游傳入或新生），
寫回回應標頭、並貫穿該請求的所有日誌。500 錯誤回應另含 `traceId` 欄位。
**回報問題時附上此碼**，即可在日誌精準定位該次請求的完整軌跡（跨服務不斷鏈）。
"""


def parse():
    """回傳 [(code, name, level_char, layer_char, desc)]，依 (layer, code) 排序。"""
    rows = []
    for name, code, desc in CONST_RE.findall(SRC.read_text(encoding="utf-8")):
        rows.append((code, name, code[0], code[1], desc))
    rows.sort(key=lambda r: (r[3], r[0]))
    return rows


def render():
    rows = parse()
    out = [HEADER]
    for layer in sorted({r[3] for r in rows}):
        out.append(f"## {LAYERS.get(layer, '?')}（layer {layer}）\n")
        out.append("| 代碼 | Level | 常數 | 意義 |")
        out.append("|---|---|---|---|")
        for code, name, lvl, lyr, desc in [r for r in rows if r[3] == layer]:
            out.append(f"| `{code}` | {LEVELS.get(lvl, lvl)} | `{name}` | {desc} |")
        out.append("")
    out.append(FOOTER)
    return "\n".join(out).rstrip() + "\n"


def main():
    content = render()
    if "--check" in sys.argv:
        current = DOC.read_text(encoding="utf-8") if DOC.exists() else ""
        if current != content:
            print("✗ docs/LOG-CODES.md 與 TwseLogCodes.cs 不一致，請重跑 scripts/gen-log-codes.py", file=sys.stderr)
            sys.exit(1)
        print("✓ LOG-CODES.md 與來源一致")
        return
    DOC.write_text(content, encoding="utf-8")
    print(f"已產生 {DOC.relative_to(ROOT)}（{len(parse())} 個代碼）")


if __name__ == "__main__":
    main()
