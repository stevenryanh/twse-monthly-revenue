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
    YYYYMM=202605 ./scripts/import-quotes.py   # 指定月份（西元 YYYYMM）
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

# 與 import-twse.py 同步的台灣50成分股快照（擷取日 2026-06-30）。
TW50 = [
    "2330", "2317", "2454", "2308", "2382", "2891", "2412", "2882", "2881", "2303",
    "3711", "2886", "2884", "2357", "2885", "1216", "2892", "2880", "2890", "3034",
    "2327", "2345", "3008", "2002", "2207", "2883", "1303", "1301", "2379", "3045",
    "4904", "2887", "5880", "1101", "2603", "3037", "2301", "2395", "6505", "2912",
    "5871", "1326", "2615", "2618", "3231", "2376", "6669", "3661", "2356", "2353",
]
# 0050（ETF 本身可交易、有股價）展開為 ETF + 成分股
INDEX_EXPANSION = {"0050": ["0050"] + TW50}


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


def main():
    codes = expand_codes(sys.argv[1:]) or INDEX_EXPANSION["0050"]
    yyyymm = os.environ.get("YYYYMM") or datetime.date.today().strftime("%Y%m")
    date_param = f"{yyyymm}01"  # STOCK_DAY 以該月任一日查整月

    print(f"▶ 匯入 {len(codes)} 檔的每日行情（月份 {yyyymm}）→ {ENDPOINT}")
    total_ok = total_fail = 0
    no_data = []
    for i, code in enumerate(codes, 1):
        try:
            d = fetch_month(code, date_param)
        except Exception as e:  # noqa: BLE001
            no_data.append((code, f"fetch 失敗 {e}"))
            continue
        if d.get("stat") != "OK" or not d.get("data"):
            no_data.append((code, d.get("stat", "無資料")))
            time.sleep(0.6)
            continue

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
                "companyCode": code,
                "tradeDate": td,
                "companyName": name,
                "openPrice": _num(row[3]),
                "highPrice": _num(row[4]),
                "lowPrice": _num(row[5]),
                "closePrice": _num(row[6]),
                "change": _num(row[7]),
                "tradeVolume": int(vol) if vol is not None else None,
            }
            try:
                ok += 1 if post_quote(body) else 0
            except Exception:  # noqa: BLE001 — 匯入工具，記錄後續行
                fail += 1
        total_ok += ok
        total_fail += fail
        print(f"  [{i}/{len(codes)}] {code}: {ok} 天" + (f"（失敗 {fail}）" if fail else ""))
        time.sleep(0.6)  # 友善節流，避免 TWSE 擋

    print(f"✓ 完成：寫入 {total_ok} 筆、失敗 {total_fail}")
    if no_data:
        print(f"  無資料/略過 {len(no_data)} 檔：" + ", ".join(c for c, _ in no_data[:15]))


if __name__ == "__main__":
    main()
