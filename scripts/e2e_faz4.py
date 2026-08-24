#!/usr/bin/env python3
"""Faz 4 uçtan uca doğrulama: başarı/finans kayıtları ve doküman
yükleme/indirme. Gerçek API'ye gerçek rollerle vurur.

Faz 3 betiğiyle aynı sözleşme: temiz tohum verisi bekler, veriyi değiştirir
(öneri onaylar, kayıt siler), bu yüzden iki koşu arasında veritabanı
sıfırlanmalı."""

import json
import os
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid

BASE = "http://localhost:5080"
PW = "T3.Creathon!2026"

# Onaylanmadan depoya yazılan dosyaların temizlendiğini doğrulamak için
# betiğin dosya sistemine bakması gerekiyor; API bunu dışarı açmıyor.
STORAGE = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "backend", "src", "T3.Api", "storage", "documents")

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


def upload(path, token, file_name, content, doc_type=None):
    """Multipart yükleme. Bağımlılık eklememek için gövde elle kuruluyor."""
    boundary = "----t3" + uuid.uuid4().hex
    query = f"?type={doc_type}" if doc_type else ""
    body = b"".join([
        f"--{boundary}\r\n".encode(),
        f'Content-Disposition: form-data; name="file"; filename="{file_name}"\r\n'.encode(),
        b"Content-Type: application/octet-stream\r\n\r\n",
        content,
        f"\r\n--{boundary}--\r\n".encode(),
    ])
    req = urllib.request.Request(BASE + path + query, data=body, method="POST")
    req.add_header("Content-Type", f"multipart/form-data; boundary={boundary}")
    req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req) as r:
            raw = r.read().decode()
            return r.status, (json.loads(raw) if raw.strip() else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try:
            return e.code, json.loads(raw)
        except json.JSONDecodeError:
            return e.code, None


def download(document_id, token):
    req = urllib.request.Request(f"{BASE}/api/documents/{document_id}/download")
    req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req) as r:
            return r.status, r.read(), dict(r.headers)
    except urllib.error.HTTPError as e:
        return e.code, e.read(), dict(e.headers)


def login(email, password=PW):
    st, b, raw = call("POST", "/api/auth/login",
                      body={"email": email, "password": password})
    if st != 200:
        print(f"!! giriş başarısız {email}: {st} {raw[:200]}")
        sys.exit(1)
    return b["accessToken"], b["user"]


def section(title):
    print(f"\n=== {title} ===")


def stored_files():
    found = []
    for root, _, names in os.walk(STORAGE):
        found.extend(os.path.join(root, n) for n in names)
    return set(found)


def achievement(kind, **kwargs):
    """Tüm alanları taşıyan gövde; ilgisiz alanlar None kalır."""
    body = {
        "kind": kind, "occurredOn": "2025-06-30", "note": None,
        "amount": None, "currency": "TRY", "fiscalYear": None, "quarter": None,
        "roundType": None, "valuation": None, "investorNames": None,
        "institution": None, "programName": None,
        "awardName": None, "organization": None, "rank": None,
        "targetCountries": None,
    }
    body.update(kwargs)
    return body


# ---------------------------------------------------------------- 1. girişler
section("1. Giriş")
admin, _ = login("admin@t3ekosistem.test")
kul, _ = login("kulucka.yoneticisi@t3ekosistem.test")
tek, _ = login("teknofest.yoneticisi@t3ekosistem.test")
kv, _ = login("karar.verici@t3ekosistem.test")
p0, p0_u = login("girisim@t3ekosistem.test")

st, page, _ = call("GET", "/api/startups?pageSize=50", admin)
by_name = {s["name"]: s["id"] for s in page["items"]}
S0 = p0_u["startupId"]                       # Anadolu Robotik — Kuluçka kapsamı
S6 = by_name["Toros Uzay Bileşenleri"]       # TEKNOFEST kapsamı

check("5 rol hesabı giriş yapabiliyor", True)
check("portal kullanıcısı Anadolu Robotik'e bağlı",
      by_name["Anadolu Robotik"] == S0, S0)

# ------------------------------------------------- 2. başarı kayıtları okuma
section("2. Başarı kayıtları — okuma ve maskeleme")

st, a_admin, _ = call("GET", f"/api/startups/{S0}/achievements", admin)
check("süper yönetici kayıtları listeliyor", st == 200, str(st))
check("tohum verisinde kayıt var", len(a_admin["items"]) >= 5,
      str(len(a_admin["items"])))
check("süper yöneticide tutarlar açık", a_admin["exactAmountsVisible"] is True)

invest = next(i for i in a_admin["items"] if i["kind"] == "Investment")
check("yatırım turunun tutarı görünüyor", invest["amount"] is not None)
check("yatırım turunun değerlemesi görünüyor", invest["valuation"] is not None)
check("maskeleme bayrağı düşük", invest["amountMasked"] is False)
check("başlık sunucuda üretiliyor", invest["title"].endswith("turu"), invest["title"])

st, a_kv, _ = call("GET", f"/api/startups/{S0}/achievements", kv)
check("karar verici kayıtları listeleyebiliyor", st == 200, str(st))
check("karar vericide satır sayısı aynı",
      len(a_kv["items"]) == len(a_admin["items"]),
      f"{len(a_kv['items'])} != {len(a_admin['items'])}")
check("karar vericide tutarlar kapalı", a_kv["exactAmountsVisible"] is False)

kv_invest = next(i for i in a_kv["items"] if i["kind"] == "Investment")
check("karar verici tutarı görmüyor", kv_invest["amount"] is None)
check("karar verici değerlemeyi görmüyor", kv_invest["valuation"] is None)
check("tutar 'yetkiniz yok' olarak işaretli", kv_invest["amountMasked"] is True)
check("karar verici satırın var olduğunu görüyor",
      kv_invest["title"] == invest["title"])

kv_award = next((i for i in a_kv["items"] if i["kind"] == "Award"), None)
if kv_award:
    check("tutarsız kayıtta maskeleme bayrağı kalkmıyor",
          kv_award["amountMasked"] is False)

st, a_pm, _ = call("GET", f"/api/startups/{S0}/achievements", kul)
check("kapsamdaki program yöneticisi tutarları görüyor",
      st == 200 and a_pm["exactAmountsVisible"] is True, str(st))

st, _, _ = call("GET", f"/api/startups/{S0}/achievements", tek)
check("kapsam dışı yönetici için 404 (403 değil)", st == 404, str(st))

st, a_own, _ = call("GET", f"/api/startups/{S0}/achievements", p0)
check("girişim kendi kayıtlarını tam görüyor",
      st == 200 and a_own["exactAmountsVisible"] is True, str(st))

st, filtered, _ = call("GET", f"/api/startups/{S0}/achievements?kind=Investment", admin)
check("tür süzgeci çalışıyor",
      filtered["items"] and all(i["kind"] == "Investment" for i in filtered["items"]))

# ------------------------------------------------- 3. başarı kayıtları yazma
section("3. Başarı kayıtları — doğrudan yazma")

new_award = achievement("Award", occurredOn="2025-09-20",
                        awardName="Savunma Sanayii Ödülü",
                        organization="SSB", rank=1, currency=None)

st, _, _ = call("POST", f"/api/startups/{S0}/achievements", p0, new_award)
check("girişim kullanıcısı doğrudan yazamıyor", st == 403, str(st))

st, _, _ = call("POST", f"/api/startups/{S0}/achievements", kv, new_award)
check("karar verici yazamıyor", st == 403, str(st))

st, _, _ = call("POST", f"/api/startups/{S0}/achievements", tek, new_award)
check("kapsam dışı yönetici yazamıyor", st in (403, 404), str(st))

st, created, raw = call("POST", f"/api/startups/{S0}/achievements", kul, new_award)
check("kapsamdaki yönetici kayıt ekliyor", st == 201, f"{st} {raw[:160]}")
check("yetkilinin girdiği kayıt doğrulanmış sayılıyor",
      created["isVerified"] is True and created["verifiedAt"] is not None)
check("ödül başlığı dereceyi taşıyor",
      created["title"] == "Savunma Sanayii Ödülü — 1. sıra", created["title"])
award_id = created["id"]

bad = achievement("Investment", amount=None, roundType="Seed")
st, err, _ = call("POST", f"/api/startups/{S0}/achievements", kul, bad)
check("tutarsız yatırım turu reddediliyor", st == 400, str(st))

bad2 = achievement("Revenue", amount=1000, fiscalYear=None)
st, _, _ = call("POST", f"/api/startups/{S0}/achievements", kul, bad2)
check("mali yılsız ciro reddediliyor", st == 400, str(st))

bad3 = achievement("Grant", amount=1000, currency="TL",
                   institution="Tubitak")
st, _, _ = call("POST", f"/api/startups/{S0}/achievements", kul, bad3)
check("üç harfli olmayan para birimi reddediliyor", st == 400, str(st))

st, _, _ = call("POST", f"/api/startups/{S0}/achievements", kul,
                achievement("Award", occurredOn="2030-01-01", awardName="Gelecek"))
check("gelecek tarihli kayıt reddediliyor", st == 400, str(st))

st, updated, raw = call("PUT", f"/api/startups/{S0}/achievements/{award_id}", kul,
                        achievement("Award", occurredOn="2025-09-20",
                                    awardName="Savunma Sanayii Ödülü",
                                    organization="SSB", rank=2, currency=None))
check("kayıt güncelleniyor", st == 200 and updated["rank"] == 2, str(st))

st, err, _ = call("PUT", f"/api/startups/{S0}/achievements/{award_id}", kul,
                  achievement("Revenue", amount=100, fiscalYear=2025))
check("kayıt türü değiştirilemiyor", st == 409, str(st))
check("tür değişimi gerekçesi açık",
      err and "tür" in err["title"].lower(), json.dumps(err, ensure_ascii=False))

st, _, _ = call("DELETE", f"/api/startups/{S0}/achievements/{award_id}", p0)
check("girişim kullanıcısı kayıt silemiyor", st == 403, str(st))

st, _, _ = call("DELETE", f"/api/startups/{S0}/achievements/{award_id}", kul)
check("yetkili kaydı pasife alıyor", st == 204, str(st))

st, after_delete, _ = call("GET", f"/api/startups/{S0}/achievements", admin)
check("silinen kayıt listede yok",
      all(i["id"] != award_id for i in after_delete["items"]))

# --------------------------------------------- 4. başarı kaydı onay akışı
section("4. Başarı kaydı — onay akışı")

st, queue, _ = call("GET", "/api/change-requests?status=Pending&pageSize=50", admin)
seeded = [i for i in queue["page"]["items"] if i["targetType"] == "Achievement"]
check("tohum kuyruğunda başarı kaydı önerisi var", len(seeded) >= 1,
      str(len(seeded)))
check("kuyruk satırı kayıt türünü yazıyor",
      all("Başarı kaydı ·" in i["targetLabel"] for i in seeded),
      json.dumps([i["targetLabel"] for i in seeded], ensure_ascii=False))

# Ciro önerisi tercih ediliyor (tutar maskelemesini gösteren satır o);
# yoksa eldeki ilk başarı kaydı önerisiyle devam ediliyor.
revenue_req = next((i for i in seeded if "Ciro" in i["targetLabel"]), seeded[0])

st, detail, _ = call("GET", f"/api/change-requests/{revenue_req['id']}", admin)
check("öneri ayrıntısı açılıyor", st == 200, str(st))
amount_field = next(f for f in detail["fields"] if f["field"] == "amount")
check("tutar alanı diff'te değişiyor", amount_field["changed"] is True)
check("yetkili tutarı diff'te görüyor",
      amount_field["masked"] is False and amount_field["after"] is not None)
check("tutar metni Türkçe biçimde",
      "." in amount_field["after"] and "TRY" in amount_field["after"],
      amount_field["after"])

st, res, raw = call("POST", f"/api/change-requests/{revenue_req['id']}/approve",
                    admin, {"note": "Bağımsız denetim raporuyla doğrulandı."})
check("başarı kaydı önerisi onaylanıyor", st == 200, f"{st} {raw[:160]}")
check("onay sonucu varlığı bildiriyor", res["appliedEntityType"] == "Achievement",
      json.dumps(res, ensure_ascii=False))

st, after_approve, _ = call("GET", f"/api/startups/{S0}/achievements", admin)
approved = next((i for i in after_approve["items"]
                 if i["id"] == res["appliedEntityId"]), None)
check("onaylanan kayıt listeye giriyor", approved is not None)
check("onaylanan kayıt doğrulanmış işaretli",
      approved and approved["isVerified"] is True)
check("onaylanan kaydın tutarı diff'te yazandan geliyor",
      approved is not None
      and (approved["amount"] is None
           or amount_field["after"].startswith(
               f"{approved['amount']:,.0f}".replace(",", "."))),
      f"{approved and approved['amount']} vs {amount_field['after']}")

# Girişimin kendi önerisi
proposal = {"targetType": "Achievement", "operation": "Create", "targetId": None,
            "startup": None, "teamMember": None,
            "achievement": achievement("Grant", amount=1250000,
                                       institution="Kosgeb",
                                       programName="KOBİGEL 2025",
                                       occurredOn="2025-04-10")}

st, submitted, raw = call("POST", "/api/change-requests", p0, proposal)
check("girişim başarı kaydı önerebiliyor", st == 201, f"{st} {raw[:160]}")

st, _, _ = call("POST", "/api/change-requests", admin, proposal)
check("yetkili öneri gönderemiyor (doğrudan yazar)", st == 403, str(st))

bad_proposal = dict(proposal)
bad_proposal["achievement"] = achievement("Investment", amount=None)
st, _, _ = call("POST", "/api/change-requests", p0, bad_proposal)
check("geçersiz gövdeli öneri kuyruğa girmiyor", st == 400, str(st))

st, _, _ = call("POST", f"/api/change-requests/{submitted['id']}/approve", kv, {})
check("karar verici onaylayamıyor", st == 403, str(st))

st, _, _ = call("POST", f"/api/change-requests/{submitted['id']}/approve", tek, {})
check("kapsam dışı yönetici için 404", st == 404, str(st))

st, res2, _ = call("POST", f"/api/change-requests/{submitted['id']}/approve", kul, {})
check("kapsamdaki yönetici onaylıyor", st == 200, str(st))

st, grants, _ = call("GET", f"/api/startups/{S0}/achievements?kind=Grant", admin)
new_grant = next((i for i in grants["items"] if i["id"] == res2["appliedEntityId"]), None)
check("onaylanan hibe kaydı oluştu", new_grant is not None)
check("hibe kurumu etiketiyle dönüyor",
      new_grant and new_grant["institutionLabel"] == "KOSGEB",
      json.dumps(new_grant, ensure_ascii=False)[:200] if new_grant else "")
check("portaldan gelen kayıt onayla doğrulanmış oldu",
      new_grant and new_grant["isVerified"] is True)

# Silme önerisi
st, del_req, raw = call("POST", "/api/change-requests", p0, {
    "targetType": "Achievement", "operation": "Delete",
    "targetId": new_grant["id"], "startup": None, "teamMember": None,
    "achievement": None})
check("girişim kayıt kaldırma önerebiliyor", st == 201, f"{st} {raw[:160]}")

st, _, _ = call("POST", f"/api/change-requests/{del_req['id']}/approve", kul, {})
check("kaldırma önerisi onaylanıyor", st == 200, str(st))

st, grants2, _ = call("GET", f"/api/startups/{S0}/achievements?kind=Grant", admin)
check("kaldırılan kayıt listeden düştü",
      all(i["id"] != new_grant["id"] for i in grants2["items"]))

# --------------------------------------------------- 5. dokümanlar — okuma
section("5. Dokümanlar — listeleme ve indirme")

st, docs, _ = call("GET", f"/api/startups/{S0}/documents", admin)
check("süper yönetici dokümanları listeliyor", st == 200, str(st))
check("tohum dokümanları yüklendi", len(docs["items"]) >= 2, str(len(docs["items"])))

pitch = next(i for i in docs["items"] if i["fileName"].endswith(".pdf"))
check("doküman türü etiketiyle dönüyor", pitch["typeLabel"] == "Sunum",
      pitch["typeLabel"])
check("boyut okunur biçimde", pitch["sizeLabel"].endswith(("B", "KB", "MB")),
      pitch["sizeLabel"])
check("yükleyenin adı çözülüyor", pitch["uploadedByName"] is not None)
check("depo yolu yanıtta yok", "storagePath" not in json.dumps(docs))

st, _, _ = call("GET", f"/api/startups/{S0}/documents", kv)
check("karar verici doküman listesini göremiyor", st == 403, str(st))

st, _, _ = call("GET", f"/api/startups/{S0}/documents", tek)
check("kapsam dışı yönetici için 404", st == 404, str(st))

st, own_docs, _ = call("GET", f"/api/startups/{S0}/documents", p0)
check("girişim kendi dokümanlarını görüyor", st == 200, str(st))

status, content, headers = download(pitch["id"], admin)
check("süper yönetici dosyayı indiriyor", status == 200, str(status))
check("indirilen dosya gerçek bir PDF", content[:5] == b"%PDF-", str(content[:8]))
check("içerik tipi uzantıdan geliyor",
      headers.get("Content-Type", "").startswith("application/pdf"),
      headers.get("Content-Type"))
check("tarayıcı içerik tipini tahmin etmiyor",
      headers.get("X-Content-Type-Options") == "nosniff",
      str(headers.get("X-Content-Type-Options")))

status, _, _ = download(pitch["id"], kv)
check("karar verici indiremiyor", status == 403, str(status))

status, _, _ = download(pitch["id"], tek)
check("kapsam dışı yönetici indiremiyor (404)", status == 404, str(status))

status, _, _ = download(str(uuid.uuid4()), admin)
check("olmayan doküman için 404", status == 404, str(status))

# --------------------------------------------------- 6. dokümanlar — yükleme
section("6. Dokümanlar — yükleme kuralları")

pdf_bytes = b"%PDF-1.4\n% kurgu demo dosyasi\n%%EOF\n"

st, up = upload(f"/api/startups/{S0}/documents", kv, "deneme.pdf", pdf_bytes)
check("karar verici yükleyemiyor", st == 403, str(st))

st, up = upload(f"/api/startups/{S0}/documents", tek, "deneme.pdf", pdf_bytes)
check("kapsam dışı yönetici için 404", st == 404, str(st))

st, up = upload(f"/api/startups/{S0}/documents", kul, "zararli.exe", pdf_bytes)
check("beyaz listede olmayan uzantı reddediliyor", st == 400, str(st))

st, up = upload(f"/api/startups/{S0}/documents", kul, "buyuk.pdf",
                b"x" * (20 * 1024 * 1024 + 1))
check("boyut sınırı uygulanıyor", st == 400, str(st))

before_files = stored_files()

st, up = upload(f"/api/startups/{S0}/documents", kul,
                "kulucka_degerlendirme_notu.pdf", pdf_bytes, doc_type="Report")
check("yetkilinin yüklemesi doğrudan kaydediliyor",
      st == 200 and up["applied"] is True, f"{st} {json.dumps(up, ensure_ascii=False)[:160]}")
check("kayıt gövdesi dönüyor", up["document"] is not None)
direct_doc = up["document"]["id"]

st, docs2, _ = call("GET", f"/api/startups/{S0}/documents", admin)
check("yüklenen doküman listede",
      any(i["id"] == direct_doc for i in docs2["items"]))

# Yol bileşeni taşıyan ad
st, up = upload(f"/api/startups/{S0}/documents", kul,
                "../../gizli/rapor.pdf", pdf_bytes, doc_type="Report")
check("yol bileşenli ad temizleniyor",
      st == 200 and up["document"]["fileName"] == "rapor.pdf",
      json.dumps(up, ensure_ascii=False)[:200])

# ------------------------------------------- 7. doküman onay akışı (portal)
section("7. Doküman — onay akışı")

st, up = upload(f"/api/startups/{S0}/documents", p0,
                "girisim_faaliyet_ozeti.pdf", pdf_bytes, doc_type="Report")
check("girişim yüklemesi onaya gidiyor",
      st == 200 and up["applied"] is False, json.dumps(up, ensure_ascii=False)[:200])
check("öneri kimliği dönüyor", up["changeRequestId"] is not None)
portal_upload = up["changeRequestId"]

st, docs3, _ = call("GET", f"/api/startups/{S0}/documents", admin)
check("onaylanmamış dosya listede görünmüyor",
      all(i["fileName"] != "girisim_faaliyet_ozeti.pdf" for i in docs3["items"]))

st, detail, _ = call("GET", f"/api/change-requests/{portal_upload}", admin)
check("doküman önerisi diff üretiyor", st == 200 and detail["fields"], str(st))
check("diff depo yolunu sızdırmıyor",
      all(f["field"] != "storagePath" for f in detail["fields"]))
name_field = next(f for f in detail["fields"] if f["field"] == "fileName")
check("dosya adı diff'te", name_field["after"] == "girisim_faaliyet_ozeti.pdf",
      str(name_field["after"]))

st, res3, _ = call("POST", f"/api/change-requests/{portal_upload}/approve", kul,
                   {"note": "Belge içeriği kontrol edildi."})
check("doküman önerisi onaylanıyor", st == 200, str(st))

st, docs4, _ = call("GET", f"/api/startups/{S0}/documents", admin)
approved_doc = next((i for i in docs4["items"] if i["id"] == res3["appliedEntityId"]), None)
check("onaylanan doküman listeye giriyor", approved_doc is not None)
check("yükleyen olarak girişim kullanıcısı yazılı",
      approved_doc and approved_doc["uploadedByName"] == "Elif Yıldırım",
      json.dumps(approved_doc, ensure_ascii=False)[:200] if approved_doc else "")

status, content, _ = download(approved_doc["id"], admin)
check("onaylanan doküman indirilebiliyor",
      status == 200 and content == pdf_bytes, str(status))

# Ret: depoya yazılan dosya temizlenmeli
st, up = upload(f"/api/startups/{S0}/documents", p0,
                "yanlis_belge.pdf", pdf_bytes, doc_type="Other")
rejected_req = up["changeRequestId"]
after_upload = stored_files()
staged = after_upload - before_files
check("reddedilecek dosya depoya yazıldı", len(staged) >= 1, str(len(staged)))

st, _, _ = call("POST", f"/api/change-requests/{rejected_req}/reject", kul,
                {"note": "Belge okunamıyor, lütfen yeniden tarayıp gönderin."})
check("doküman önerisi reddedilebiliyor", st == 200, str(st))

remaining = stored_files()
check("reddedilen önerinin dosyası depodan siliniyor",
      len(remaining) == len(after_upload) - 1,
      f"{len(after_upload)} -> {len(remaining)}")

st, docs5, _ = call("GET", f"/api/startups/{S0}/documents", admin)
check("reddedilen dosya listeye girmiyor",
      all(i["fileName"] != "yanlis_belge.pdf" for i in docs5["items"]))

# Kaldırma önerisi
st, rm_req, raw = call("POST", "/api/change-requests", p0, {
    "targetType": "Document", "operation": "Delete", "targetId": direct_doc,
    "startup": None, "teamMember": None, "achievement": None})
check("girişim doküman kaldırma önerebiliyor", st == 201, f"{st} {raw[:160]}")

st, _, _ = call("POST", "/api/change-requests", p0, {
    "targetType": "Document", "operation": "Create", "targetId": None,
    "startup": None, "teamMember": None, "achievement": None})
check("doküman yüklemesi JSON ucundan gönderilemiyor", st == 400, str(st))

st, _, _ = call("POST", f"/api/change-requests/{rm_req['id']}/approve", kul, {})
check("kaldırma önerisi onaylanıyor", st == 200, str(st))

st, docs6, _ = call("GET", f"/api/startups/{S0}/documents", admin)
check("kaldırılan doküman listeden düştü",
      all(i["id"] != direct_doc for i in docs6["items"]))

# --------------------------------------------------------- 8. denetim izi
section("8. Denetim izi")

st, logs, _ = call("GET", "/api/audit-logs?pageSize=200", admin)
actions = [row["action"] for row in logs["items"]]

for action in ["Achievement.Create", "Achievement.Update", "Achievement.Delete",
               "Document.Create", "Document.Download", "Document.Delete"]:
    check(f"denetim izinde {action} var", action in actions,
          json.dumps(sorted(set(actions)), ensure_ascii=False))

st, dl_logs, _ = call("GET", "/api/audit-logs?action=Document.Download&pageSize=50", admin)
check("indirme kayıtları filtrelenebiliyor",
      dl_logs["items"] and all(r["action"] == "Document.Download"
                               for r in dl_logs["items"]))
check("indirmeyi kimin yaptığı iz üzerinde",
      dl_logs["items"][0]["actorName"] is not None,
      json.dumps(dl_logs["items"][0], ensure_ascii=False)[:200])

st, _, _ = call("GET", "/api/audit-logs", kul)
check("program yöneticisi denetim izini göremiyor", st == 403, str(st))

# --------------------------------------------- 9. kartla tutarlılık ve silme
section("9. Girişim kartı ve silme zinciri")

st, card, _ = call("GET", f"/api/startups/{S0}", admin)
check("kart doküman sayısını gösteriyor", card["documentCount"] >= 1,
      str(card["documentCount"]))
check("kart başarı özeti taşıyor", card["achievements"]["totalCount"] >= 5,
      json.dumps(card["achievements"], ensure_ascii=False))
check("kartta tutarlar açık", card["achievements"]["totalInvestment"] is not None)

st, kv_card, _ = call("GET", f"/api/startups/{S0}", kv)
check("karar vericide kart tutarları maskeli",
      kv_card["achievements"]["totalInvestment"] is None)
check("karar vericide doküman sayısı sıfırlanıyor",
      kv_card["documentCount"] == 0, str(kv_card["documentCount"]))
check("karar verici kayıt sayısını görüyor",
      kv_card["achievements"]["totalCount"] == card["achievements"]["totalCount"])

st, timeline, _ = call("GET", f"/api/startups/{S0}/timeline", admin)
check("onaylanan kayıt zaman çizelgesine düşüyor",
      any(e["kind"] == "Revenue" and e["amount"] == 18400000
          for e in timeline["entries"]),
      json.dumps([e["title"] for e in timeline["entries"]], ensure_ascii=False)[:200])

st, deleted, raw = call("DELETE", f"/api/startups/{S6}", admin)
check("girişim silme zinciri çalışıyor", st == 200, f"{st} {raw[:160]}")
check("zincir başarı kayıtlarını da kapatıyor", deleted["achievements"] >= 1,
      json.dumps(deleted, ensure_ascii=False))
check("zincir dokümanları da kapatıyor", deleted["documents"] >= 1,
      json.dumps(deleted, ensure_ascii=False))

st, _, _ = call("GET", f"/api/startups/{S6}/achievements", admin)
check("silinen girişimin kayıtları erişilemez", st == 404, str(st))

st, _, _ = call("GET", f"/api/startups/{S6}/documents", admin)
check("silinen girişimin dokümanları erişilemez", st == 404, str(st))

# ------------------------------------------------------------------- özet
print("\n" + "=" * 60)
print(f"{ok_count} kontrol geçti, {len(failures)} başarısız")
for f in failures:
    print("  FAIL " + f)
sys.exit(1 if failures else 0)
