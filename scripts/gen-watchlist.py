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

API_BASE = os.environ.get("API_BASE", "http://localhost:5080").rstrip("/")
OUT = os.environ.get("WATCHLIST_OUT", "watchlist.md")
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
        out += ["| 代號 | 名稱 | 波段分 | 報酬/風險 | 週期 | 區間位置 | 每張約 |",
                "|---|---|---|---|---|---|---|"]
        for r in bottom:
            out.append(
                f"| {r['companyCode']} | {r['companyName']} | {r['swingScore']} | "
                f"{r['riskAdjustedReturn']} | {r['cycleDays']}天 | "
                f"{r['pricePositionPercent']}% | {lot(r)} |")
    else:
        out.append("今天**沒有**『即將見底』的好時機——不一定要買，建議觀望。")

    out += ["", "## 其他高分（時機普通/偏高，僅供參考）", "",
            "| 代號 | 名稱 | 波段分 | 進場時機 | 每張約 |",
            "|---|---|---|---|---|"]
    for r in others:
        out.append(
            f"| {r['companyCode']} | {r['companyName']} | {r['swingScore']} | "
            f"{r.get('entryTiming') or '—'} | {lot(r)} |")

    with open(OUT, "w", encoding="utf-8") as f:
        f.write("\n".join(out) + "\n")
    print(f"✓ 觀察名單已寫入 {OUT}（即將見底 {len(bottom)} 檔）")


if __name__ == "__main__":
    main()
