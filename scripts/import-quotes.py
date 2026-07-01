#!/usr/bin/env python3
"""
從臺灣證券交易所匯入「每日收盤行情」（買賣股票投報用）。

來源：STOCK_DAY（單檔一個月每日 OHLC）https://www.twse.com.tw/exchangeReport/STOCK_DAY
走後端 POST /api/quotes（依主鍵 upsert）逐筆寫入，可重複執行不重複建檔。
交易日以民國 YYYMMDD 整數保真儲存；漲跌價差帶正負，供「每元當日報酬 = 漲跌 ÷ 昨收」計算。

用法：
    ./scripts/import-quotes.py                 # 預設：0050（ETF 本身）+ 台灣50成分股，當月
    ./scripts/import-quotes.py 2330 2317       # 指定代碼
    ./scripts/import-quotes.py 0050            # 0050 展開為 ETF + 成分股
    ./scripts/import-quotes.py pool            # 0050∪0056∪00878∪00713（每日選股範圍）
    MONTHS=6 ./scripts/import-quotes.py        # 抓最近 6 個月（波段分析建議 3–6 個月）
    YYYYMM=202605 ./scripts/import-quotes.py   # 只抓指定單一月份（西元 YYYYMM）
"""
import datetime
import json
import os
import sys
import time
import urllib.request

API_BASE = os.environ.get("API_BASE", "http://localhost:5080").rstrip("/")
ENDPOINT = f"{API_BASE}/api/quotes"
STOCK_DAY = "https://www.twse.com.tw/exchangeReport/STOCK_DAY?response=json&date={date}&stockNo={code}"

# ETF 成分股快照集中於 scripts/etf_pools.py（0050/0056/00878/00713，共用免漂移）。
from etf_pools import ETF_CONSTITUENTS, ETF_TICKERS, constituents_union

# ETF 本身可交易、有股價 → 展開為「ETF 代碼 + 其成分股」。
# "pool" = 四檔 ETF 代碼 + 成分股聯集（每日選股範圍）。
INDEX_EXPANSION = {etf: [etf] + members for etf, members in ETF_CONSTITUENTS.items()}
INDEX_EXPANSION["pool"] = ETF_TICKERS + constituents_union()


def expand_codes(args):
    out, seen = [], set()
    for code in args:
        for c in INDEX_EXPANSION.get(code, [code]):
            if c not in seen:
                seen.add(c)
                out.append(c)
    return out


def _num(s):
    """TWSE 數字字串 → float：去千分位逗號；'--'/空/X → None。"""
    s = (s or "").strip().replace(",", "")
    if s in ("", "--", "X", "x"):
        return None
    try:
        return float(s)
    except ValueError:
        return None


def roc_date_to_int(s):
    """'115/06/29' → 1150629（民國 YYYMMDD 整數）。"""
    try:
        y, m, d = s.strip().split("/")
        return int(y) * 10000 + int(m) * 100 + int(d)
    except (ValueError, AttributeError):
        return None


def post_quote(body):
    data = json.dumps(body).encode("utf-8")
    req = urllib.request.Request(
        ENDPOINT, data=data, headers={"Content-Type": "application/json"}, method="POST")
    with urllib.request.urlopen(req, timeout=15) as r:
        return r.status in (200, 201)


def fetch_month(code, yyyymmdd):
    url = STOCK_DAY.format(date=yyyymmdd, code=code)
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=20) as resp:
        return json.load(resp)


def month_date_params():
    """要抓的月份 date 參數清單（每月一個 YYYYMMDD）。
    YYYYMM 指定 → 只抓該月；否則抓最近 MONTHS 個月（預設 3）。"""
    if os.environ.get("YYYYMM"):
        return [f"{os.environ['YYYYMM']}01"]
    months = max(1, int(os.environ.get("MONTHS", "3")))
    today = datetime.date.today()
    out, y, m = [], today.year, today.month
    for _ in range(months):
        out.append(f"{y:04d}{m:02d}01")
        m -= 1
        if m == 0:
            m, y = 12, y - 1
    return out


def import_one(code, date_param):
    """抓單檔單月並逐日 upsert；回傳 (寫入天數, 失敗數)。stat 非 OK 回 (None, 0)。"""
    try:
        d = fetch_month(code, date_param)
    except Exception:  # noqa: BLE001
        return None, 0
    if d.get("stat") != "OK" or not d.get("data"):
        return None, 0
    # title 形如「115年06月 2330 台積電   各日成交資訊」；移除固定詞後最後一段為名稱
    name_parts = d.get("title", "").replace("各日成交資訊", "").split()
    name = name_parts[-1][:60] if len(name_parts) >= 3 else None
    ok = fail = 0
    for row in d["data"]:
        # 欄位：日期, 成交股數, 成交金額, 開盤價, 最高價, 最低價, 收盤價, 漲跌價差, 成交筆數, 註記
        td = roc_date_to_int(row[0])
        if td is None:
            continue
        vol = _num(row[1])
        body = {
            "companyCode": code, "tradeDate": td, "companyName": name,
            "openPrice": _num(row[3]), "highPrice": _num(row[4]), "lowPrice": _num(row[5]),
            "closePrice": _num(row[6]), "change": _num(row[7]),
            "tradeVolume": int(vol) if vol is not None else None,
        }
        try:
            ok += 1 if post_quote(body) else 0
        except Exception:  # noqa: BLE001 — 匯入工具，記錄後續行
            fail += 1
    return ok, fail


def main():
    codes = expand_codes(sys.argv[1:]) or INDEX_EXPANSION["0050"]
    dates = month_date_params()

    print(f"▶ 匯入 {len(codes)} 檔 × {len(dates)} 個月的每日行情 → {ENDPOINT}")
    total_ok = total_fail = 0
    no_data = []
    for i, code in enumerate(codes, 1):
        code_ok = code_fail = 0
        for dp in dates:
            ok, fail = import_one(code, dp)
            if ok is None:
                no_data.append((code, dp))
            else:
                code_ok += ok
                code_fail += fail
            time.sleep(0.6)  # 友善節流，避免 TWSE 擋
        total_ok += code_ok
        total_fail += code_fail
        print(f"  [{i}/{len(codes)}] {code}: {code_ok} 天" + (f"（失敗 {code_fail}）" if code_fail else ""))

    print(f"✓ 完成：寫入 {total_ok} 筆、失敗 {total_fail}")
    if no_data:
        print(f"  部分月份無資料 {len(no_data)} 筆（屬正常，如該月尚未開市）")


if __name__ == "__main__":
    main()
