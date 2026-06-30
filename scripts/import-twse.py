#!/usr/bin/env python3
"""
從臺灣證券交易所 OpenAPI（t187ap05_L，上市公司每月營業收入）匯入資料。

走後端 POST /api/revenues（依主鍵 upsert）逐筆寫入，因此可重複執行不重複建檔。
資料保真：民國年月日、各欄位皆以來源原值入庫，不在匯入時換算或失真。

用法：
    ./scripts/import-twse.py                  # 預設 API http://localhost:5080
    API_BASE=http://localhost:5080 ./scripts/import-twse.py
"""
import json
import os
import sys
import urllib.request

SOURCE = "https://openapi.twse.com.tw/v1/opendata/t187ap05_L"
API_BASE = os.environ.get("API_BASE", "http://localhost:5080").rstrip("/")
ENDPOINT = f"{API_BASE}/api/revenues"


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
    """TWSE 來源欄位 → 後端 CreateRevenueRequest（camelCase）。"""
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
    print(f"▶ 下載來源：{SOURCE}")
    with urllib.request.urlopen(SOURCE, timeout=30) as resp:
        rows = json.load(resp)
    print(f"  取得 {len(rows)} 筆")

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
        except Exception as e:  # noqa: BLE001 — 匯入工具，記錄後續行
            fail += 1
            failures.append((raw.get("公司代號"), str(e)))
        if i % 100 == 0:
            print(f"  …{i}/{len(rows)}（成功 {ok}、失敗 {fail}）")

    print(f"✓ 完成：成功 {ok}、失敗 {fail}")
    if failures:
        print("  失敗樣本（前 10）：")
        for code, reason in failures[:10]:
            print(f"    {code}: {reason}")
        sys.exit(1)


if __name__ == "__main__":
    main()
