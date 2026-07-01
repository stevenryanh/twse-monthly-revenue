#!/usr/bin/env python3
"""
從臺灣證券交易所 OpenAPI（t187ap05_L，上市公司每月營業收入）匯入資料。

走後端 POST /api/revenues（依主鍵 upsert）逐筆寫入,因此可重複執行不重複建檔。
資料保真:民國年月日、各欄位皆以來源原值入庫,不在匯入時換算或失真。

用法:
    ./scripts/import-twse.py                 # 匯入全部上市公司
    ./scripts/import-twse.py 2330 1101       # 只匯入指定代碼
    ./scripts/import-twse.py 0050            # 0050 展開為台灣50成分股
    ./scripts/import-twse.py 0056 00878      # 指定 ETF：展開為其成分股
    ./scripts/import-twse.py pool            # 0050∪0056∪00878∪00713 成分股聯集

註:ETF 本身沒有「每月營收」,不在 t187ap05_L 中;指定 ETF/pool 時改餵其成分股。
   成分快照見 scripts/etf_pools.py。
"""
import json
import os
import sys
import urllib.request

SOURCE = "https://openapi.twse.com.tw/v1/opendata/t187ap05_L"
API_BASE = os.environ.get("API_BASE", "http://localhost:5080").rstrip("/")
ENDPOINT = f"{API_BASE}/api/revenues"

# ETF 成分股快照集中於 scripts/etf_pools.py（0050/0056/00878/00713，共用免漂移）。
from etf_pools import ETF_CONSTITUENTS, constituents_union

# 指數/ETF 代碼 → 成分股展開表；"pool" = 四檔聯集。
# 營收：ETF 本身無每月營收,故只展開為成分股(不含 ETF 代碼本身)。
INDEX_EXPANSION = dict(ETF_CONSTITUENTS)
INDEX_EXPANSION["pool"] = constituents_union()


def expand_codes(args):
    """把使用者輸入的代碼展開:指數代碼換成成分股,其餘原樣保留(去重保序)。"""
    out, seen = [], set()
    for code in args:
        for c in INDEX_EXPANSION.get(code, [code]):
            if c not in seen:
                seen.add(c)
                out.append(c)
    return out


def _int(s):
    s = (s or "").strip()
    try:
        return int(s)
    except ValueError:
        return None


def _dec(s):
    s = (s or "").strip()
    try:
        return float(s)
    except ValueError:
        return None


def _str(s):
    s = (s or "").strip()
    return s or None


def to_request(r):
    """TWSE 來源欄位 → 後端 CreateRevenueRequest(camelCase)。"""
    return {
        "companyCode": (r.get("公司代號") or "").strip(),
        "dataYearMonth": _int(r.get("資料年月")),
        "reportDate": _int(r.get("出表日期")),
        "companyName": (r.get("公司名稱") or "").strip(),
        "industry": _str(r.get("產業別")),
        "currentMonthRevenue": _int(r.get("營業收入-當月營收")),
        "lastMonthRevenue": _int(r.get("營業收入-上月營收")),
        "lastYearMonthRevenue": _int(r.get("營業收入-去年當月營收")),
        "moMPercent": _dec(r.get("營業收入-上月比較增減(%)")),
        "yoYPercent": _dec(r.get("營業收入-去年同月增減(%)")),
        "cumCurrentRevenue": _int(r.get("累計營業收入-當月累計營收")),
        "cumLastYearRevenue": _int(r.get("累計營業收入-去年累計營收")),
        "cumDiffPercent": _dec(r.get("累計營業收入-前期比較增減(%)")),
        "remark": _str(r.get("備註")),
    }


def main():
    wanted = expand_codes(sys.argv[1:])  # 空 = 全部

    print(f"▶ 下載來源:{SOURCE}")
    with urllib.request.urlopen(SOURCE, timeout=30) as resp:
        rows = json.load(resp)
    print(f"  取得 {len(rows)} 筆")

    if wanted:
        wset = set(wanted)
        rows_sel = [r for r in rows if (r.get("公司代號") or "").strip() in wset]
        found = {(r.get("公司代號") or "").strip() for r in rows_sel}
        missing = [c for c in wanted if c not in found]
        print(f"▶ 指定 {len(wanted)} 檔 → 來源命中 {len(rows_sel)} 筆")
        if missing:
            # 0050(ETF)等無每月營收者會落在這:屬正常,非錯誤。
            print(f"  ⚠ 來源查無(無每月營收,如 ETF):{', '.join(missing)}")
        rows = rows_sel

    if not rows:
        print("✗ 沒有可匯入的資料。")
        sys.exit(1)

    print(f"▶ 匯入至 {ENDPOINT}")
    ok = fail = 0
    failures = []
    for i, raw in enumerate(rows, 1):
        body = json.dumps(to_request(raw)).encode("utf-8")
        req = urllib.request.Request(
            ENDPOINT, data=body,
            headers={"Content-Type": "application/json"}, method="POST")
        try:
            with urllib.request.urlopen(req, timeout=15) as r:
                if r.status in (200, 201):
                    ok += 1
                else:
                    fail += 1
                    failures.append((raw.get("公司代號"), r.status))
        except Exception as e:  # noqa: BLE001 — 匯入工具,記錄後續行
            fail += 1
            failures.append((raw.get("公司代號"), str(e)))
        if len(rows) > 50 and i % 100 == 0:
            print(f"  …{i}/{len(rows)}(成功 {ok}、失敗 {fail})")

    print(f"✓ 完成:成功 {ok}、失敗 {fail}")
    if failures:
        print("  失敗樣本(前 10):")
        for code, reason in failures[:10]:
            print(f"    {code}: {reason}")
        sys.exit(1)


if __name__ == "__main__":
    main()
