#!/usr/bin/env python3
"""
產生每日觀察名單 watchlist.md：從「易入手波段分」自動挑出『即將見底』與高分標的。

需 API 在跑（由 daily-fetch.sh 在抓取後、關 API 前呼叫）。
僅供參考、非投資建議；過去不代表未來。
"""
import datetime
import json
import os
import urllib.request

from etf_pools import pools_of

API_BASE = os.environ.get("API_BASE", "http://localhost:5080").rstrip("/")
OUT = os.environ.get("WATCHLIST_OUT", "watchlist.md")
OUT_HTML = os.environ.get("WATCHLIST_HTML", "watchlist.html")
# 小資總預算（NT$，預設 10 萬）→ 每股價上限 = 預算 ÷ 1000，只列買得起一張的
BUDGET = int(os.environ.get("WATCHLIST_BUDGET", "100000"))


def fetch_ranking(sort="swingscore", top=40, max_price=None):
    url = f"{API_BASE}/api/quotes/ranking?sort={sort}&top={top}"
    if max_price:
        url += f"&maxPrice={max_price}"
    with urllib.request.urlopen(url, timeout=20) as r:
        return json.load(r)


def lot(x):
    c = x.get("lastClose") or 0
    return f"{round(c * 1000 / 10000, 1)} 萬"


def pools(x):
    tags = pools_of(x.get("companyCode", ""))
    return "·".join(tags) if tags else "—"


def _esc(s):
    return (str(s).replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;"))


def write_html(today, budget_wan, bottom, others):
    """自帶樣式、離線可看的可攜式觀察名單（手機友善，可 LINE/AirDrop 分享）。"""
    def rows_html(items, entry_col):
        tr = []
        for r in items:
            cells = [r["companyCode"], _esc(r["companyName"]), _esc(pools(r)), r["swingScore"]]
            if entry_col:
                cells.append(_esc(r.get("entryTiming") or "—"))
            else:
                cells += [r["riskAdjustedReturn"], f"{r['cycleDays']}天",
                          f"{r['pricePositionPercent']}%"]
            cells.append(_esc(lot(r)))
            tds = "".join(f"<td>{c}</td>" for c in cells)
            tr.append(f"<tr>{tds}</tr>")
        return "\n".join(tr)

    bottom_html = (
        "<table><thead><tr><th>代號</th><th>名稱</th><th>所屬池</th><th>波段分</th>"
        "<th>報酬/風險</th><th>週期</th><th>區間位置</th><th>每張約</th></tr></thead><tbody>"
        + rows_html(bottom, entry_col=False) + "</tbody></table>"
        if bottom else
        "<p class='none'>今天沒有「即將見底」的好時機——不一定要買，建議觀望。</p>")

    others_html = (
        "<table><thead><tr><th>代號</th><th>名稱</th><th>所屬池</th><th>波段分</th>"
        "<th>進場時機</th><th>每張約</th></tr></thead><tbody>"
        + rows_html(others, entry_col=True) + "</tbody></table>")

    html = f"""<!DOCTYPE html>
<html lang="zh-Hant"><head><meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>每日觀察名單 {today}</title>
<style>
 body {{ font-family: system-ui, -apple-system, "PingFang TC", sans-serif; max-width: 720px; margin: 0 auto; padding: 18px; background: #f5f5f5; color: #222; }}
 h1 {{ font-size: 1.25rem; margin: 0 0 4px; }}
 .sub {{ color: #888; font-size: .82rem; margin: 0 0 10px; }}
 .disc {{ font-size: .74rem; color: #b26a00; background: #fff8e1; border: 1px solid #ffe0a3; border-radius: 8px; padding: 8px 12px; line-height: 1.6; margin: 0 0 16px; }}
 h2 {{ font-size: 1rem; margin: 18px 0 8px; }}
 table {{ width: 100%; border-collapse: collapse; background: #fff; border-radius: 10px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,.1); font-size: .82rem; }}
 th, td {{ padding: 8px 10px; text-align: right; border-bottom: 1px solid #f0f0f0; white-space: nowrap; }}
 th {{ background: #fafafa; color: #666; font-weight: 600; }}
 td:nth-child(2), th:nth-child(2), td:nth-child(3), th:nth-child(3) {{ text-align: left; }}
 tbody tr:last-child td {{ border-bottom: none; }}
 .none {{ color: #b26a00; background: #fff8e1; border: 1px solid #ffe0a3; border-radius: 8px; padding: 10px 14px; }}
 .foot {{ color: #aaa; font-size: .72rem; text-align: center; margin: 22px 0 6px; }}
</style></head><body>
<h1>每日觀察名單 — {today}</h1>
<p class="sub">總預算 ≤ {budget_wan} 萬 ｜ 由「易入手波段分」自動產生（報酬÷波動÷波段週期×離低點）</p>
<div class="disc">⚠️ 僅供參考、非投資建議；過去不代表未來。紀律：單筆 ≤30%、沒好時機就觀望、只用閒錢、少賺有賺就好。</div>
<h2>⭐ 即將見底（值得留意進場）</h2>
{bottom_html}
<h2>其他高分（時機普通/偏高，僅供參考）</h2>
{others_html}
<p class="foot">此為離線可攜文檔;資料截至 {today}。所屬池：權值=0050、高股息=0056、ESG高息=00878、高息低波=00713。</p>
</body></html>
"""
    with open(OUT_HTML, "w", encoding="utf-8") as f:
        f.write(html)


def main():
    max_price = BUDGET // 1000  # 買得起一張的每股價上限
    rows = fetch_ranking(max_price=max_price)
    today = datetime.date.today().strftime("%Y-%m-%d")
    budget_wan = round(BUDGET / 10000, 1)
    bottom = [r for r in rows if r.get("entryTiming") == "即將見底"]
    others = [r for r in rows if r.get("entryTiming") != "即將見底"][:8]

    out = [
        f"# 每日觀察名單 — {today}（總預算 ≤ {budget_wan} 萬）",
        "",
        "> 由「易入手波段分」自動產生（報酬÷波動÷波段週期×離低點），僅列買得起一張的。"
        "**僅供參考、非投資建議；過去不代表未來。**",
        "> 紀律：單筆 ≤30%、沒好時機就觀望、只用閒錢、少賺有賺就好。",
        "",
        "## ⭐ 即將見底（值得留意進場）",
        "",
    ]
    if bottom:
        out += ["| 代號 | 名稱 | 所屬池 | 波段分 | 報酬/風險 | 週期 | 區間位置 | 每張約 |",
                "|---|---|---|---|---|---|---|---|"]
        for r in bottom:
            out.append(
                f"| {r['companyCode']} | {r['companyName']} | {pools(r)} | {r['swingScore']} | "
                f"{r['riskAdjustedReturn']} | {r['cycleDays']}天 | "
                f"{r['pricePositionPercent']}% | {lot(r)} |")
    else:
        out.append("今天**沒有**『即將見底』的好時機——不一定要買，建議觀望。")

    out += ["", "## 其他高分（時機普通/偏高，僅供參考）", "",
            "| 代號 | 名稱 | 所屬池 | 波段分 | 進場時機 | 每張約 |",
            "|---|---|---|---|---|---|"]
    for r in others:
        out.append(
            f"| {r['companyCode']} | {r['companyName']} | {pools(r)} | {r['swingScore']} | "
            f"{r.get('entryTiming') or '—'} | {lot(r)} |")

    with open(OUT, "w", encoding="utf-8") as f:
        f.write("\n".join(out) + "\n")
    write_html(today, budget_wan, bottom, others)
    print(f"✓ 觀察名單已寫入 {OUT} + {OUT_HTML}（即將見底 {len(bottom)} 檔）")


if __name__ == "__main__":
    main()
