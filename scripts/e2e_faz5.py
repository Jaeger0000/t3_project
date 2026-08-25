#!/usr/bin/env python3
"""Faz 5 uçtan uca doğrulama: ekosistem panosu, CSV dışa aktarma, AI karar
destek ucu ve MCP sunucusu.

Önceki fazlarla aynı sözleşme: temiz tohum verisi bekler. Bu betik veriyi
**değiştirmez** (yalnızca okur ve denetim izine satır düşürür), bu yüzden
e2e_faz3/e2e_faz4'ten sonra da çalıştırılabilir; ama sayı beklentileri temiz
tohum verisine göre yazıldığı için tercih edilen sıra kendi turudur."""

import csv
import io
import json
import sys
import urllib.error
import urllib.parse
import urllib.request

BASE = "http://localhost:5080"
PW = "T3.Creathon!2026"

ok_count = 0
failures = []


def check(label, condition, detail=""):
    global ok_count
    if condition:
        ok_count += 1
        print(f"  ok   {label}")
    else:
        failures.append(f"{label} — {detail}")
        print(f"  FAIL {label} — {detail}")


def call(method, path, token=None, body=None):
    url = BASE + urllib.parse.quote(path, safe="/?&=,:")
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Accept", "application/json")
    if data is not None:
        req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req) as r:
            raw = r.read().decode()
            return r.status, (json.loads(raw) if raw.strip() else None), raw
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try:
            return e.code, json.loads(raw), raw
        except json.JSONDecodeError:
            return e.code, None, raw


def raw_get(path, token):
    """Gövdesi JSON olmayan uçlar (CSV) için ham indirme."""
    req = urllib.request.Request(BASE + path)
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req) as r:
            return r.status, r.read(), dict(r.headers)
    except urllib.error.HTTPError as e:
        return e.code, e.read(), dict(e.headers)


def rpc(method, token, params=None, message_id=1):
    body = {"jsonrpc": "2.0", "id": message_id, "method": method}
    if params is not None:
        body["params"] = params
    return call("POST", "/mcp", token, body)


def login(email, password=PW):
    st, b, raw = call("POST", "/api/auth/login",
                      body={"email": email, "password": password})
    if st != 200:
        print(f"!! giriş başarısız {email}: {st} {raw[:200]}")
        sys.exit(1)
    return b["accessToken"], b["user"]


def section(title):
    print(f"\n=== {title} ===")


def parse_csv(payload):
    text = payload.decode("utf-8-sig")
    return list(csv.reader(io.StringIO(text), delimiter=";"))


# ---------------------------------------------------------------- 1. girişler
section("1. Giriş")
admin, _ = login("admin@t3ekosistem.test")
kul, _ = login("kulucka.yoneticisi@t3ekosistem.test")
tek, _ = login("teknofest.yoneticisi@t3ekosistem.test")
kv, _ = login("karar.verici@t3ekosistem.test")
p0, p0_u = login("girisim@t3ekosistem.test")
check("5 rol hesabı giriş yapabiliyor", True)

st, page, _ = call("GET", "/api/startups?pageSize=100", admin)
by_name = {s["name"]: s["id"] for s in page["items"]}
S0 = p0_u["startupId"]
check("demo ölçeği 30+ girişim", page["totalCount"] >= 30, str(page["totalCount"]))

# ------------------------------------------------------- 2. ekosistem karnesi
section("2. Ekosistem karnesi")

st, stats, raw = call("GET", "/api/reports/ecosystem", admin)
check("karne 200 dönüyor", st == 200, f"{st} {raw[:160]}")

totals = stats["totals"]
check("girişim sayısı listeyle tutarlı",
      totals["startups"] == page["totalCount"],
      f'{totals["startups"]} != {page["totalCount"]}')
check("faal + mezun toplam girişimi aşmıyor",
      totals["activeStartups"] + totals["graduatedStartups"] <= totals["startups"])
check("sektör dilimleri toplamı girişim sayısına eşit",
      sum(s["count"] for s in stats["bySector"]) == totals["startups"],
      json.dumps(stats["bySector"], ensure_ascii=False)[:200])
check("durum dilimleri toplamı girişim sayısına eşit",
      sum(s["count"] for s in stats["byStatus"]) == totals["startups"])
check("sektör etiketleri Türkçe",
      any(s["label"] == "Savunma" for s in stats["bySector"]),
      json.dumps([s["label"] for s in stats["bySector"]], ensure_ascii=False))
check("şehir dilimi en çok 8 satır",
      len(stats["byCity"]) <= 8, str(len(stats["byCity"])))
check("program dilimi tüm programları kapsıyor",
      len(stats["byProgram"]) == 5, str(len(stats["byProgram"])))
check("yatırım turu dağılımı dolu", len(stats["investmentByRound"]) >= 3)
check("yatırım yıl serisi artan sırada",
      [s["key"] for s in stats["investmentByYear"]]
      == sorted(s["key"] for s in stats["investmentByYear"]))
check("yatırım turu toplamları genel toplamla tutarlı",
      abs(sum(s["total"] for s in stats["investmentByRound"])
          - totals["totalInvestment"]) < 0.01,
      f'{sum(s["total"] for s in stats["investmentByRound"])} != {totals["totalInvestment"]}')
check("yıllık yatırım toplamları genel toplamla tutarlı",
      abs(sum(s["total"] for s in stats["investmentByYear"])
          - totals["totalInvestment"]) < 0.01)
check("en çok yatırım alan listesi 5 satır",
      len(stats["topByInvestment"]) == 5, str(len(stats["topByInvestment"])))
check("sıralama azalan",
      [r["investment"] for r in stats["topByInvestment"]]
      == sorted((r["investment"] for r in stats["topByInvestment"]), reverse=True))
check("yatırım alan girişim sayısı toplamı aşmıyor",
      totals["investedStartups"] <= totals["startups"])
check("para birimi TRY", stats["currency"] == "TRY", stats["currency"])

# --------------------------------------------------- 3. karnede rol kapsamı
section("3. Karnede kapsam ve maskeleme")

st, kul_stats, _ = call("GET", "/api/reports/ecosystem", kul)
st, tek_stats, _ = call("GET", "/api/reports/ecosystem", tek)
check("kuluçka yöneticisi ekosistemin tamamını görmüyor",
      kul_stats["totals"]["startups"] < totals["startups"],
      f'{kul_stats["totals"]["startups"]} / {totals["startups"]}')
check("teknofest yöneticisi de daraltılmış",
      tek_stats["totals"]["startups"] < totals["startups"])
check("iki program yöneticisinin programları farklı",
      {s["label"] for s in kul_stats["byProgram"]}
      != {s["label"] for s in tek_stats["byProgram"]},
      json.dumps([s["label"] for s in kul_stats["byProgram"]], ensure_ascii=False))
check("program yöneticisinin yatırımı ekosistem toplamından küçük",
      kul_stats["totals"]["totalInvestment"] < totals["totalInvestment"])

st, p0_stats, _ = call("GET", "/api/reports/ecosystem", p0)
check("girişim kullanıcısının karnesi yalnızca kendisi",
      p0_stats["totals"]["startups"] == 1, str(p0_stats["totals"]["startups"]))

st, kv_stats, _ = call("GET", "/api/reports/ecosystem", kv)
check("karar verici tüm satırları görüyor",
      kv_stats["totals"]["startups"] == totals["startups"])
check("karar verici AGREGAT tutarı görüyor",
      kv_stats["amountsVisible"] is True and kv_stats["totals"]["totalInvestment"] is not None,
      json.dumps(kv_stats["totals"], ensure_ascii=False)[:200])
check("karar vericide TEKİL girişim tutarı maskeli",
      all(r["investment"] is None for r in kv_stats["topByInvestment"]),
      json.dumps(kv_stats["topByInvestment"], ensure_ascii=False)[:250])
check("karar verici sıralamayı yine de görüyor",
      [r["name"] for r in kv_stats["topByInvestment"]]
      == [r["name"] for r in stats["topByInvestment"]])

st, filtered, _ = call("GET", "/api/reports/ecosystem?sector=Defense", admin)
check("sektör süzgeci karneyi daraltıyor",
      0 < filtered["totals"]["startups"] < totals["startups"],
      str(filtered["totals"]["startups"]))
check("süzülmüş karnede yalnızca o sektör var",
      [s["key"] for s in filtered["bySector"]] == ["Defense"],
      json.dumps(filtered["bySector"], ensure_ascii=False))

program_id = stats["byProgram"][0]["key"]
st, by_program, _ = call("GET", f"/api/reports/ecosystem?programId={program_id}", admin)
check("program süzgeci çalışıyor",
      by_program["totals"]["startups"] == stats["byProgram"][0]["count"],
      f'{by_program["totals"]["startups"]} != {stats["byProgram"][0]["count"]}')

st, _, _ = call("GET", "/api/reports/ecosystem")
check("kimliksiz istek reddediliyor", st == 401, str(st))

# --------------------------------------------------------- 4. CSV dışa aktarma
section("4. CSV dışa aktarma")

st, payload, headers = raw_get("/api/reports/export", admin)
check("dışa aktarma 200 dönüyor", st == 200, str(st))
check("içerik türü CSV", "text/csv" in headers.get("Content-Type", ""),
      headers.get("Content-Type", ""))
check("nosniff başlığı var",
      headers.get("X-Content-Type-Options") == "nosniff",
      str(headers.get("X-Content-Type-Options")))
check("dosya adı tarihli",
      "t3-girisimler-" in headers.get("Content-Disposition", ""),
      headers.get("Content-Disposition", ""))
check("UTF-8 BOM ile başlıyor", payload[:3] == b"\xef\xbb\xbf", str(payload[:6]))

rows = parse_csv(payload)
check("başlık satırı + tüm girişimler",
      len(rows) == totals["startups"] + 1, f"{len(rows)} satır")
check("ayraç noktalı virgül", len(rows[0]) > 10, str(len(rows[0])))
check("başlıklar Türkçe", rows[0][0] == "Girişim" and "Vergi No" in rows[0],
      json.dumps(rows[0], ensure_ascii=False)[:200])
check("Türkçe karakterler bozulmamış",
      any("ş" in cell or "ğ" in cell or "İ" in cell for row in rows for cell in row))

header = rows[0]
tax_col = header.index("Vergi No")
mail_col = header.index("İletişim e-posta")
inv_col = header.index("Toplam yatırım")
admin_rows = {row[0]: row for row in rows[1:]}
sample = admin_rows["Anadolu Robotik"]
check("süper yönetici vergi numarasını görüyor",
      sample[tax_col].isdigit(), sample[tax_col])
check("tutar tr-TR biçiminde",
      "." in sample[inv_col] and "yetkiniz" not in sample[inv_col], sample[inv_col])

st, kv_payload, _ = raw_get("/api/reports/export", kv)
kv_rows = {row[0]: row for row in parse_csv(kv_payload)[1:]}
kv_sample = kv_rows["Anadolu Robotik"]
check("karar vericide vergi no maskeli",
      kv_sample[tax_col] == "yetkiniz yok", kv_sample[tax_col])
check("karar vericide iletişim maskeli",
      kv_sample[mail_col] == "yetkiniz yok", kv_sample[mail_col])
check("karar vericide tutar maskeli",
      kv_sample[inv_col] == "yetkiniz yok", kv_sample[inv_col])
check("karar vericide ad ve sektör açık",
      kv_sample[header.index("Sektör")] != "yetkiniz yok")

st, kul_payload, _ = raw_get("/api/reports/export", kul)
kul_rows = parse_csv(kul_payload)
check("program yöneticisinin dosyası kapsamıyla sınırlı",
      len(kul_rows) - 1 == kul_stats["totals"]["startups"],
      f'{len(kul_rows) - 1} != {kul_stats["totals"]["startups"]}')
check("kapsam dışı girişim dosyada yok",
      all(row[0] != "Toros Uzay Bileşenleri" for row in kul_rows[1:]))
kul_sample = {row[0]: row for row in kul_rows[1:]}["Anadolu Robotik"]
check("program yöneticisinde vergi no maskeli, tutar açık",
      kul_sample[tax_col] == "yetkiniz yok" and "yetkiniz" not in kul_sample[inv_col],
      f"{kul_sample[tax_col]} / {kul_sample[inv_col]}")

st, defense_payload, _ = raw_get("/api/reports/export?sector=Defense", admin)
check("dışa aktarma süzgeci uyguluyor",
      len(parse_csv(defense_payload)) - 1 == filtered["totals"]["startups"],
      str(len(parse_csv(defense_payload)) - 1))

st, phone_payload, _ = raw_get("/api/reports/export?q=Anadolu", admin)
phone_rows = parse_csv(phone_payload)
phone_col = phone_rows[0].index("İletişim telefon")
check("formül gibi başlayan hücre kaçırılmış",
      phone_rows[1][phone_col].startswith("'+"), phone_rows[1][phone_col])

st, _, _ = raw_get("/api/reports/export", None)
check("kimliksiz dışa aktarma reddediliyor", st == 401, str(st))

# ---------------------------------------------------------------- 5. AI ucu
section("5. AI karar destek")

st, answer, raw = call("POST", "/api/ai/ask", admin,
                       {"question": "Savunma sektöründe kaç girişim var?"})
check("soru ucu 200 dönüyor", st == 200, f"{st} {raw[:200]}")
check("yanıt metni boş değil", len(answer["answer"]) > 10, answer["answer"][:120])
check("yanıt kaynak listesi taşıyor", len(answer["sources"]) >= 1,
      json.dumps(answer["sources"], ensure_ascii=False)[:200])
check("kaynaklar araç adı içeriyor",
      all("tool" in s and "summary" in s for s in answer["sources"]))
check("mod bildiriliyor", answer["mode"] in ("Model", "Local"), answer["mode"])
check("ekosistem aracı çağrılmış",
      any(s["tool"] == "ecosystem_stats" for s in answer["sources"]),
      json.dumps([s["tool"] for s in answer["sources"]]))

st, listed, _ = call("POST", "/api/ai/ask", admin,
                     {"question": "TEKNOFEST'ten geçmiş girişimler hangileri?"})
check("program adı arama aracına çevriliyor",
      any(s["tool"] == "search_startups" for s in listed["sources"]),
      json.dumps([s["tool"] for s in listed["sources"]]))

st, approvals, _ = call("POST", "/api/ai/ask", admin,
                        {"question": "Bekleyen onay istekleri neler?"})
check("onay sorusu onay aracını çağırıyor",
      any(s["tool"] == "list_pending_approvals" for s in approvals["sources"]),
      json.dumps([s["tool"] for s in approvals["sources"]]))

st, kv_answer, _ = call("POST", "/api/ai/ask", kv,
                        {"question": "Ekosistemde kaç girişim var?"})
check("karar verici de soru sorabiliyor", st == 200, str(st))
kv_defense = [s for s in kv_answer["sources"] if s["tool"] == "ecosystem_stats"]
check("karar vericinin yanıtı da kaynak taşıyor", len(kv_defense) >= 1)

st, p0_answer, _ = call("POST", "/api/ai/ask", p0,
                        {"question": "Kaç girişim var?"})
check("girişim kullanıcısının yanıtı kendi kapsamıyla sınırlı",
      "1 girişim" in p0_answer["answer"], p0_answer["answer"][:150])

st, err, _ = call("POST", "/api/ai/ask", admin, {"question": "  "})
check("boş soru 400 dönüyor", st == 400, str(st))
st, err, _ = call("POST", "/api/ai/ask", admin, {"question": "x" * 600})
check("çok uzun soru 400 dönüyor", st == 400, str(st))
st, _, _ = call("POST", "/api/ai/ask", None, {"question": "test"})
check("kimliksiz soru reddediliyor", st == 401, str(st))

st, summary, raw = call("GET", f"/api/ai/startups/{S0}/summary", admin)
check("girişim özeti 200 dönüyor", st == 200, f"{st} {raw[:200]}")
check("özet metni girişim adını içeriyor",
      "Anadolu Robotik" in summary["summary"], summary["summary"][:120])
check("özet dayanak listesi taşıyor", len(summary["highlights"]) >= 3,
      json.dumps(summary["highlights"], ensure_ascii=False)[:200])
check("özet tutar görünürlüğünü bildiriyor",
      summary["exactAmountsVisible"] is True)

st, kv_summary, _ = call("GET", f"/api/ai/startups/{S0}/summary", kv)
check("karar vericinin özetinde tutar yok",
      kv_summary["exactAmountsVisible"] is False
      and "22.000.000" not in kv_summary["summary"],
      kv_summary["summary"][:200])
check("karar vericinin özeti yine de içerik taşıyor",
      len(kv_summary["highlights"]) >= 2,
      json.dumps(kv_summary["highlights"], ensure_ascii=False)[:200])

# Kapsam dışı kayıt ada göre değil **kapsamdan** seçiliyor: sabit ad, önceki
# betiklerin (faz3 soft delete zinciri) o kaydı pasife almasıyla kırılıyordu ve
# kontrol ürünü değil veri durumunu ölçüyordu.
st, kul_page, _ = call("GET", "/api/startups?pageSize=100", kul)
kul_scope = {s["id"] for s in kul_page["items"]}
out_of_scope = next((sid for sid in by_name.values() if sid not in kul_scope), None)
check("kapsam dışı bir girişim bulundu", out_of_scope is not None,
      f"kapsam={len(kul_scope)} / toplam={len(by_name)}")
if out_of_scope:
    st, _, _ = call("GET", f"/api/ai/startups/{out_of_scope}/summary", kul)
    check("kapsam dışı girişimin özeti 404", st == 404, str(st))

# --------------------------------------------------------------- 6. MCP ucu
section("6. MCP sunucusu")

st, init, raw = rpc("initialize", admin, {})
check("initialize 200 dönüyor", st == 200, f"{st} {raw[:160]}")
check("protokol sürümü bildiriliyor",
      init["result"]["protocolVersion"].startswith("20"),
      json.dumps(init["result"]))
check("araç yeteneği ilan ediliyor", "tools" in init["result"]["capabilities"])
check("jsonrpc alanı doğru", init["jsonrpc"] == "2.0")

st, tools, _ = rpc("tools/list", admin, message_id=2)
names = [t["name"] for t in tools["result"]["tools"]]
check("altı araç listeleniyor", len(names) == 6, json.dumps(names))
for expected in ["search_startups", "get_startup_card", "get_program_history",
                 "ecosystem_stats", "list_pending_approvals"]:
    check(f"araç var: {expected}", expected in names, json.dumps(names))
first = tools["result"]["tools"][0]
check("araç şeması JSON Schema", first["inputSchema"]["type"] == "object",
      json.dumps(first["inputSchema"])[:160])

st, called, raw = rpc("tools/call", admin,
                      {"name": "ecosystem_stats", "arguments": {"sector": "Defense"}},
                      message_id=3)
check("tools/call 200 dönüyor", st == 200, f"{st} {raw[:160]}")
check("araç sonucu hata değil", called["result"]["isError"] is False)
payload = json.loads(called["result"]["content"][0]["text"])
check("MCP sonucu REST ile aynı sayıyı veriyor",
      payload["totals"]["startups"] == filtered["totals"]["startups"],
      f'{payload["totals"]["startups"]} != {filtered["totals"]["startups"]}')

st, card_call, _ = rpc("tools/call", admin,
                       {"name": "get_startup_card", "arguments": {"startupId": S0}},
                       message_id=4)
check("kart aracı çalışıyor",
      json.loads(card_call["result"]["content"][0]["text"])["name"] == "Anadolu Robotik")

st, kv_call, _ = rpc("tools/call", kv,
                     {"name": "get_startup_card", "arguments": {"startupId": S0}},
                     message_id=5)
kv_card = json.loads(kv_call["result"]["content"][0]["text"])
check("MCP aynı maskelemeyi uyguluyor",
      kv_card["taxNumber"] is None and kv_card["contactEmail"] is None,
      json.dumps(kv_card)[:200])

st, scoped, _ = rpc("tools/call", kul,
                    {"name": "get_startup_card", "arguments": {"startupId": out_of_scope}},
                    message_id=6)
check("MCP kapsam dışı kaydı vermiyor",
      scoped["result"]["isError"] is True,
      json.dumps(scoped["result"], ensure_ascii=False)[:200])

st, unknown, _ = rpc("tools/call", admin,
                     {"name": "olmayan_arac", "arguments": {}}, message_id=7)
check("bilinmeyen araç isError ile dönüyor",
      unknown["result"]["isError"] is True)

st, bad_method, _ = rpc("resources/list", admin, message_id=8)
check("bilinmeyen yöntem JSON-RPC hatası", bad_method["error"]["code"] == -32601,
      json.dumps(bad_method))

st, missing, _ = rpc("tools/call", admin, message_id=9)
check("params eksikse -32602", missing["error"]["code"] == -32602,
      json.dumps(missing))

st, _, _ = call("POST", "/mcp", None, {"jsonrpc": "2.0", "id": 1, "method": "tools/list"})
check("kimliksiz MCP isteği reddediliyor", st == 401, str(st))

st, ping, _ = rpc("ping", admin, message_id=10)
check("ping yanıtlanıyor", st == 200 and "result" in ping, str(st))

# --------------------------------------------------------------- 7. denetim
section("7. Denetim izi")

st, logs, _ = call("GET", "/api/audit-logs?action=Report.Export&pageSize=50", admin)
check("CSV indirmeleri denetim izine düşüyor",
      logs["totalCount"] >= 3, str(logs["totalCount"]))
check("kayıt satır sayısını taşıyor",
      any("rows" in json.dumps(item).lower() for item in logs["items"]),
      json.dumps(logs["items"][:1], ensure_ascii=False)[:250])

st, ai_logs, _ = call("GET", "/api/audit-logs?action=Assistant.Ask&pageSize=50", admin)
check("AI soruları denetim izine düşüyor",
      ai_logs["totalCount"] >= 5, str(ai_logs["totalCount"]))
check("hangi araçların çağrıldığı iz kaydında",
      any("ecosystem_stats" in json.dumps(item) for item in ai_logs["items"]),
      json.dumps(ai_logs["items"][:1], ensure_ascii=False)[:250])

st, _, _ = call("GET", "/api/audit-logs", kv)
check("denetim izi hâlâ yalnızca yöneticide", st == 403, str(st))

# ------------------------------------------------------- 8. tutarlılık kontrolü
section("8. Pano ile liste tutarlılığı")

st, defense_list, _ = call("GET", "/api/startups?sector=Defense&pageSize=100", admin)
check("panodaki sektör sayısı listeyle birebir",
      defense_list["totalCount"] == filtered["totals"]["startups"],
      f'{defense_list["totalCount"]} != {filtered["totals"]["startups"]}')

st, top_list, _ = call("GET", "/api/startups?sort=MostInvestment&pageSize=5", admin)
check("panodaki sıralama liste sıralamasıyla aynı",
      [s["name"] for s in top_list["items"]]
      == [r["name"] for r in stats["topByInvestment"]],
      json.dumps([s["name"] for s in top_list["items"]], ensure_ascii=False))

st, program_list, _ = call(
    "GET", f"/api/startups?programId={program_id}&pageSize=100", admin)
check("panodaki program sayısı listeyle birebir",
      program_list["totalCount"] == stats["byProgram"][0]["count"],
      f'{program_list["totalCount"]} != {stats["byProgram"][0]["count"]}')

# ------------------------------------------------------------------- özet
print("\n" + "=" * 60)
print(f"{ok_count} kontrol geçti, {len(failures)} başarısız")
for f in failures:
    print("  FAIL " + f)
sys.exit(1 if failures else 0)
