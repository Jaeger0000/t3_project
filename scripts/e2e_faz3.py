#!/usr/bin/env python3
"""Faz 3 uçtan uca doğrulama: onay akışı, denetim izi, kullanıcı yönetimi,
soft delete zinciri. Gerçek API'ye gerçek rollerle vurur."""

import json
import sys
import time
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


def login(email, password=PW):
    st, b, raw = call("POST", "/api/auth/login",
                      body={"email": email, "password": password})
    if st != 200:
        print(f"!! giriş başarısız {email}: {st} {raw[:200]}")
        sys.exit(1)
    return b["accessToken"], b["user"]


def section(title):
    print(f"\n=== {title} ===")


# ---------------------------------------------------------------- 1. girişler
section("1. Giriş ve oturum")
admin, admin_u = login("admin@t3ekosistem.test")
kul, kul_u = login("kulucka.yoneticisi@t3ekosistem.test")
tek, tek_u = login("teknofest.yoneticisi@t3ekosistem.test")
kv, kv_u = login("karar.verici@t3ekosistem.test")
p0, p0_u = login("girisim@t3ekosistem.test")
p2, p2_u = login("girisim.marmara@t3ekosistem.test")

check("6 rol hesabı giriş yapabiliyor", True)
check("portal kullanıcısı bir girişime bağlı", p0_u.get("startupId") is not None,
      json.dumps(p0_u))
check("süper yönetici girişime bağlı değil", admin_u.get("startupId") is None)
check("hiçbir oturum yanıtında parola özeti yok",
      "passwordHash" not in json.dumps(admin_u).lower().replace(" ", ""))

# ------------------------------------------------------------- 2. kuyruk kapsamı
section("2. Onay kuyruğu kapsamı")
st, q_admin, _ = call("GET", "/api/change-requests?pageSize=100", admin)
check("süper yönetici kuyruğu 200", st == 200, str(st))
total = q_admin["page"]["totalCount"]
# Sayılar sabit yazılmıyor: her faz tohum kuyruğuna yeni öneri ekliyor, sabit
# sayı o fazda kırılıp gerçek bir hatayı gizleyecek gürültü üretiyordu.
# Değişmeyen söz, üç durumun toplamının toplam sayıya eşit olması.
check("durum sayıları toplamı toplam isteğe eşit",
      q_admin["pendingCount"] + q_admin["approvedCount"] + q_admin["rejectedCount"]
      == total,
      f"{q_admin['pendingCount']}/{q_admin['approvedCount']}/{q_admin['rejectedCount']}"
      f" != {total}")
check("kuyrukta karara bağlanmış geçmiş de var (1 onaylı, 1 reddedilmiş)",
      (q_admin["approvedCount"], q_admin["rejectedCount"]) == (1, 1),
      f"{q_admin['approvedCount']}/{q_admin['rejectedCount']}")
check("bekleyen öneri sayısı demoyu anlatmaya yetiyor",
      q_admin["pendingCount"] >= 6, str(q_admin["pendingCount"]))

all_ids = {i["id"] for i in q_admin["page"]["items"]}

st, q_kul, _ = call("GET", "/api/change-requests?pageSize=100", kul)
st2, q_tek, _ = call("GET", "/api/change-requests?pageSize=100", tek)
kul_ids = {i["id"] for i in q_kul["page"]["items"]}
tek_ids = {i["id"] for i in q_tek["page"]["items"]}
check("iki program yöneticisinin kuyruğu ayrık", kul_ids.isdisjoint(tek_ids),
      f"kesişim={kul_ids & tek_ids}")
check("iki kuyruk birleşimi tüm istekleri kapsıyor",
      kul_ids | tek_ids == all_ids,
      f"eksik={all_ids - (kul_ids | tek_ids)}")
check("her iki yönetici de boş kuyruk görmüyor", kul_ids and tek_ids)

st, q_kv, _ = call("GET", "/api/change-requests?pageSize=100", kv)
check("karar verici onay kuyruğunda satır görmüyor",
      st == 200 and q_kv["page"]["totalCount"] == 0,
      f"{st} {q_kv['page']['totalCount'] if st == 200 else ''}")

st, q_p0, _ = call("GET", "/api/change-requests?pageSize=100", p0)
p0_ids = {i["id"] for i in q_p0["page"]["items"]}
check("portal kullanıcısı yalnızca kendi isteklerini görüyor",
      st == 200 and p0_ids and p0_ids <= all_ids
      and all(i["startupId"] == p0_u["startupId"] for i in q_p0["page"]["items"]),
      f"{st} {len(p0_ids)}")

st, q_pend, _ = call("GET", "/api/change-requests?status=Pending&pageSize=100", admin)
check("durum süzgeci satırları daraltır ama sayıları değil",
      all(i["status"] == "Pending" for i in q_pend["page"]["items"])
      and q_pend["pendingCount"] == q_admin["pendingCount"]
      and q_pend["approvedCount"] == q_admin["approvedCount"],
      json.dumps(q_pend["page"]["items"][:1]))

# -------------------------------------------------------- 3. diff ve maskeleme
section("3. Diff görünümü ve KVKK maskelemesi")
tax_req = None
for item in q_admin["page"]["items"]:
    if item["targetType"] != "Startup" or item["status"] != "Pending":
        continue
    _, d, _ = call("GET", f"/api/change-requests/{item['id']}", admin)
    if any(f["field"] == "taxNumber" and f["changed"] for f in d["fields"]):
        tax_req = (item, d)
        break

check("vergi numarası değiştiren bekleyen bir öneri var", tax_req is not None)
if tax_req:
    item, admin_view = tax_req
    tax_admin = next(f for f in admin_view["fields"] if f["field"] == "taxNumber")
    check("süper yönetici vergi numarasının ham değerini görüyor",
          tax_admin["masked"] is False and tax_admin["after"],
          json.dumps(tax_admin))

    owner_pm = kul if item["id"] in kul_ids else tek
    other_pm = tek if item["id"] in kul_ids else kul
    st, pm_view, _ = call("GET", f"/api/change-requests/{item['id']}", owner_pm)
    check("kapsamdaki program yöneticisi detayı görebiliyor", st == 200, str(st))
    tax_pm = next(f for f in pm_view["fields"] if f["field"] == "taxNumber")
    check("program yöneticisine vergi numarası maskeleniyor",
          tax_pm["masked"] is True and tax_pm["before"] is None
          and tax_pm["after"] is None,
          json.dumps(tax_pm))
    check("maskeleme 'değişti' bilgisini gizlemiyor", tax_pm["changed"] is True)
    check("değişen alan sayısı role göre değişmiyor",
          pm_view["changedFieldCount"] == admin_view["changedFieldCount"],
          f"{pm_view['changedFieldCount']} != {admin_view['changedFieldCount']}")
    check("gövde okunabilir işaretli", admin_view["isReadable"] is True)

    st, _, _ = call("GET", f"/api/change-requests/{item['id']}", other_pm)
    check("kapsam dışı istek 403 değil 404 döner (varlık sızmıyor)", st == 404, str(st))

    st, _, _ = call("GET", f"/api/change-requests/{item['id']}", kv)
    check("karar verici öneri detayını göremez", st == 404, str(st))

# --------------------------------------------------------------- 4. gönderim
section("4. Öneri gönderme kuralları")
profile = {
    "name": "Anadolu Robotik", "legalName": None, "taxNumber": None,
    "foundedOn": None, "sector": "Defense", "technologyAreas": ["Robotik"],
    "productDescription": "E2E denemesi", "website": None, "logoUrl": None,
    "city": "Ankara", "contactEmail": None, "contactPhone": None, "status": None,
}
member = {
    "fullName": "E2E Deneme", "role": "CTO", "email": "e2e@ornek.test",
    "phone": None, "linkedInUrl": None, "isPrimaryContact": False, "leftOn": None,
}

st, _, raw = call("POST", "/api/change-requests", admin,
                  {"targetType": "Startup", "operation": "Update", "targetId": None,
                   "startup": profile, "teamMember": None})
check("süper yönetici öneri gönderemez (doğrudan yazar)", st == 403, f"{st} {raw[:120]}")

st, _, raw = call("POST", "/api/change-requests", p0,
                  {"targetType": "Startup", "operation": "Delete", "targetId": None,
                   "startup": profile, "teamMember": None})
check("girişim kendi kaydını silmeyi öneremez", st == 400, f"{st} {raw[:120]}")

st, _, raw = call("POST", "/api/change-requests", p0,
                  {"targetType": "Startup", "operation": "Update", "targetId": None,
                   "startup": profile, "teamMember": None})
check("aynı girişim için ikinci bekleyen profil önerisi 409", st == 409,
      f"{st} {raw[:160]}")

st, created, raw = call("POST", "/api/change-requests", p0,
                        {"targetType": "TeamMember", "operation": "Create",
                         "targetId": None, "startup": None, "teamMember": member})
check("yeni ekip üyesi önerisi kabul edilir", st == 201, f"{st} {raw[:160]}")
new_member_req = created["id"] if st == 201 else None

st, _, raw = call("POST", "/api/change-requests", p0,
                  {"targetType": "TeamMember", "operation": "Create", "targetId": None,
                   "startup": None,
                   "teamMember": dict(member, email="gecersiz")})
check("geçersiz gövde doğrudan yazmayla aynı kurallarda reddedilir", st == 400,
      f"{st} {raw[:120]}")

st, _, raw = call("POST", "/api/change-requests", p0,
                  {"targetType": "Document", "operation": "Create", "targetId": None,
                   "startup": None, "teamMember": None})
check("akışa dahil olmayan hedef türü reddedilir", st == 400, f"{st} {raw[:120]}")

# ------------------------------------------------------------- 5. karar verme
section("5. Onay ve ret")
if tax_req:
    item, admin_view = tax_req
    req_id = item["id"]
    owner_pm = kul if req_id in kul_ids else tek
    other_pm = tek if req_id in kul_ids else kul
    target_startup = item["startupId"]
    new_tax = next(f["after"] for f in admin_view["fields"]
                   if f["field"] == "taxNumber")

    st, _, _ = call("POST", f"/api/change-requests/{req_id}/approve", p0, {"note": None})
    check("portal kullanıcısı kendi önerisini onaylayamaz", st == 403, str(st))

    st, _, _ = call("POST", f"/api/change-requests/{req_id}/approve", kv, {"note": None})
    check("karar verici onaylayamaz", st == 403, str(st))

    st, _, _ = call("POST", f"/api/change-requests/{req_id}/approve", other_pm,
                    {"note": None})
    check("kapsam dışı yönetici onaylayamaz (404)", st == 404, str(st))

    st, res, raw = call("POST", f"/api/change-requests/{req_id}/approve", owner_pm,
                        {"note": "Ticaret sicil belgesiyle doğrulandı."})
    check("kapsamdaki yönetici onaylayabiliyor", st == 200, f"{st} {raw[:160]}")
    check("onay yanıtı uygulanan kaydı bildiriyor",
          st == 200 and res["status"] == "Approved"
          and res.get("appliedEntityId") is not None,
          raw[:160])

    st, _, raw = call("POST", f"/api/change-requests/{req_id}/approve", owner_pm,
                      {"note": None})
    check("ikinci onay 409 döner", st == 409, f"{st} {raw[:120]}")

    st, card, _ = call("GET", f"/api/startups/{target_startup}", admin)
    check("onaylanan değişiklik girişim kartına işlendi",
          st == 200 and card.get("taxNumber") == new_tax,
          f"{card.get('taxNumber')} != {new_tax}")

    st, card_pm, _ = call("GET", f"/api/startups/{target_startup}", owner_pm)
    check("kartta vergi numarası yöneticiye hâlâ maskeli",
          st == 200 and card_pm.get("taxNumber") is None, str(card_pm.get("taxNumber")))

# ret: teknofest kapsamındaki bekleyen bir profil önerisi
reject_id = None
for i in q_admin["page"]["items"]:
    if i["status"] == "Pending" and i["id"] in tek_ids and (not tax_req
                                                            or i["id"] != tax_req[0]["id"]):
        reject_id = i["id"]
        break
check("reddedilecek bekleyen bir öneri bulundu", reject_id is not None)
if reject_id:
    st, _, raw = call("POST", f"/api/change-requests/{reject_id}/reject", tek,
                      {"note": "kısa"})
    check("gerekçesiz/kısa ret reddedilir", st == 400, f"{st} {raw[:120]}")

    st, res, raw = call("POST", f"/api/change-requests/{reject_id}/reject", tek,
                        {"note": "Beyan edilen bilgi ticaret sicil kaydıyla uyuşmuyor."})
    check("gerekçeli ret kabul edilir",
          st == 200 and res["status"] == "Rejected", f"{st} {raw[:160]}")

    st, det, _ = call("GET", f"/api/change-requests/{reject_id}", tek)
    check("ret gerekçesi kayıtta duruyor",
          st == 200 and det["reviewNote"] and det["reviewedByName"],
          json.dumps(det.get("reviewNote")))

# portal kullanıcısı reddedilen isteğinin gerekçesini görebiliyor mu
portal_by_startup = {p0_u["startupId"]: p0, p2_u["startupId"]: p2}
st, q_rej, _ = call("GET", "/api/change-requests?status=Rejected&pageSize=100", admin)
mine = [i for i in q_rej["page"]["items"] if i["startupId"] in portal_by_startup]
check("reddedilmiş isteği olan bir portal hesabı var", bool(mine),
      json.dumps([i["startupId"] for i in q_rej["page"]["items"]]))
if mine:
    tok = portal_by_startup[mine[0]["startupId"]]
    st, det, _ = call("GET", f"/api/change-requests/{mine[0]['id']}", tok)
    check("portal kendi reddedilen isteğinin gerekçesini okuyabiliyor",
          st == 200 and det["status"] == "Rejected" and det["reviewNote"],
          f"{st} {json.dumps(det.get('reviewNote')) if st == 200 else ''}")
    check("portal kullanıcısına karar düğmeleri kapalı",
          st == 200 and det["canReview"] is False, str(st))

# --------------------------------------------------------------- 6. denetim izi
section("6. Denetim izi")
st, _, _ = call("GET", "/api/audit-logs", kul)
check("program yöneticisi denetim izini göremez", st == 403, str(st))
st, _, _ = call("GET", "/api/audit-logs", p0)
check("portal kullanıcısı denetim izini göremez", st == 403, str(st))
st, _, _ = call("GET", "/api/audit-logs", kv)
check("karar verici denetim izini göremez", st == 403, str(st))

st, logs, raw = call("GET", "/api/audit-logs?pageSize=200", admin)
check("süper yönetici denetim izini okuyabiliyor", st == 200, str(st))
actions = {r["action"] for r in logs["items"]}
check("gönderim izi yazılmış", "ChangeRequest.Submit" in actions, str(sorted(actions)))
check("onay izi yazılmış", "ChangeRequest.Approve" in actions, str(sorted(actions)))
check("ret izi yazılmış", "ChangeRequest.Reject" in actions, str(sorted(actions)))
check("veri değişikliği ayrı satır olarak da yazılmış",
      "Startup.Update" in actions, str(sorted(actions)))
check("aktör adları çözülmüş",
      all(r["actorName"] and not r["actorName"].startswith("(kayıt yok")
          for r in logs["items"]),
      str([r["actorName"] for r in logs["items"]][:5]))
check("denetim izinde parola özeti geçmiyor", "passwordhash" not in raw.lower())

st, pref, _ = call("GET", "/api/audit-logs?action=ChangeRequest&pageSize=200", admin)
check("eylem süzgeci önek eşleşmesi yapıyor",
      st == 200 and pref["items"]
      and all(r["action"].startswith("ChangeRequest") for r in pref["items"]),
      str(st))

# --------------------------------------------------------- 7. kullanıcı yönetimi
section("7. Kullanıcı yönetimi")
st, _, _ = call("GET", "/api/users", kul)
check("program yöneticisi kullanıcı yönetemez", st == 403, str(st))
st, _, _ = call("GET", "/api/users", p0)
check("portal kullanıcısı kullanıcı yönetemez", st == 403, str(st))

st, users, raw = call("GET", "/api/users?pageSize=100", admin)
# Sabit sayı beklenmiyor. Betik kendi hesabını geride bırakıyor (pasife alınan
# kullanıcı listede kalır, silinmez) ve `== 8` ikinci koşuda düşüyordu: kontrol
# ürünü değil, önceki koşunun izini ölçmüş oluyordu. Aranan şey listenin
# **tohum hesaplarının tamamını** içermesi.
seed_accounts = {
    "admin@t3ekosistem.test", "kulucka.yoneticisi@t3ekosistem.test",
    "teknofest.yoneticisi@t3ekosistem.test", "karar.verici@t3ekosistem.test",
    "girisim@t3ekosistem.test", "girisim.marmara@t3ekosistem.test",
    "girisim.toros@t3ekosistem.test", "girisim.trakya@t3ekosistem.test",
}
listed = {u["email"] for u in users.get("items", [])}
check("süper yönetici kullanıcıları listeleyebiliyor",
      st == 200 and seed_accounts.issubset(listed),
      f"{st} eksik: {sorted(seed_accounts - listed)}")
check("kullanıcı listesinde parola özeti yok", "passwordhash" not in raw.lower())

st, _, raw = call("POST", "/api/users", admin,
                  {"email": "yeni@t3ekosistem.test", "fullName": "Yeni Kullanıcı",
                   "role": "StartupUser", "password": "Guclu.Sifre1",
                   "startupId": None, "programIds": None})
check("girişimsiz girişim kullanıcısı oluşturulamaz", st == 400, f"{st} {raw[:120]}")

st, _, raw = call("POST", "/api/users", admin,
                  {"email": "yeni@t3ekosistem.test", "fullName": "Yeni Kullanıcı",
                   "role": "ProgramManager", "password": "kisa1",
                   "startupId": None, "programIds": None})
check("zayıf parola reddedilir", st == 400, f"{st} {raw[:120]}")

st, progs, _ = call("GET", "/api/programs", admin)
prog_items = progs["items"] if isinstance(progs, dict) and "items" in progs else progs
prog_id = prog_items[0]["id"]

st, new_user, raw = call("POST", "/api/users", admin,
                         {"email": "yeni.yonetici@t3ekosistem.test",
                          "fullName": "Yeni Yönetici", "role": "ProgramManager",
                          "password": "Guclu.Sifre1", "startupId": None,
                          "programIds": [prog_id]})
check("geçerli kullanıcı oluşturulabiliyor", st == 201, f"{st} {raw[:160]}")
check("oluşturma yanıtında parola özeti yok", "passwordhash" not in raw.lower())
new_id = new_user["id"] if st == 201 else None

st, _, raw = call("POST", "/api/users", admin,
                  {"email": "YENİ.yonetici@t3ekosistem.test".replace("İ", "I"),
                   "fullName": "Kopya", "role": "ProgramManager",
                   "password": "Guclu.Sifre1", "startupId": None,
                   "programIds": [prog_id]})
check("e-posta tekilliği harf duyarsız", st == 409, f"{st} {raw[:120]}")

st, _, raw = call("DELETE", f"/api/users/{admin_u['id']}", admin)
check("kullanıcı kendi hesabını pasife alamaz", st == 400, f"{st} {raw[:120]}")

st, _, raw = call("PUT", f"/api/users/{admin_u['id']}", admin,
                  {"fullName": admin_u["fullName"], "role": "ProgramManager",
                   "startupId": None, "programIds": [prog_id], "isActive": True})
check("kullanıcı kendi rolünü düşüremez", st == 400, f"{st} {raw[:120]}")

if new_id:
    time.sleep(61)  # giriş hız sınırı penceresi (dakikada 10) sıfırlansın
    tok, _ = login("yeni.yonetici@t3ekosistem.test", "Guclu.Sifre1")
    check("yeni hesapla giriş yapılabiliyor", bool(tok))

    st, _, _ = call("DELETE", f"/api/users/{new_id}", admin)
    check("hesap pasife alınabiliyor", st == 204, str(st))

    st, b, raw = call("POST", "/api/auth/login",
                      body={"email": "yeni.yonetici@t3ekosistem.test",
                            "password": "Guclu.Sifre1"})
    check("pasif hesap giriş yapamaz", st in (400, 401), f"{st} {raw[:120]}")

    st, _, raw = call("DELETE", f"/api/users/{new_id}", admin)
    check("zaten pasif hesabın ikinci pasifleştirmesi 409", st == 409, f"{st} {raw[:120]}")

    st, _, raw = call("PUT", f"/api/users/{new_id}/password", admin,
                      {"password": "Baska.Sifre9"})
    check("şifre atama çalışıyor", st == 200, f"{st} {raw[:120]}")

    st, logs2, _ = call("GET", "/api/audit-logs?action=User&pageSize=100", admin)
    acts = {r["action"] for r in logs2["items"]}
    check("kullanıcı işlemleri denetim izine düşüyor",
          {"User.Create", "User.Deactivate", "User.SetPassword"} <= acts,
          str(sorted(acts)))
    st, _, raw2 = call("GET", "/api/audit-logs?action=User.SetPassword", admin)
    check("şifre atama izi parola taşımıyor",
          "sifre" not in raw2.lower() and "password" not in raw2.lower().replace(
              "user.setpassword", ""),
          raw2[:200])

# ------------------------------------------------------- 8. soft delete zinciri
section("8. Girişim silme ve soft delete zinciri")
st, startups, _ = call("GET", "/api/startups?pageSize=100", admin)
referenced = {i["startupId"] for i in q_admin["page"]["items"]}
victim = next((s for s in startups["items"] if s["id"] not in referenced), None)
check("silinecek girişim seçildi", victim is not None)

if victim:
    st, _, _ = call("DELETE", f"/api/startups/{victim['id']}", kul)
    check("program yöneticisi girişim silemez", st == 403, str(st))
    st, _, _ = call("DELETE", f"/api/startups/{victim['id']}", p0)
    check("portal kullanıcısı girişim silemez", st == 403, str(st))

    st, res, raw = call("DELETE", f"/api/startups/{victim['id']}", admin)
    check("süper yönetici girişimi pasife alabiliyor", st == 200, f"{st} {raw[:160]}")
    check("zincir sayıları raporlanıyor",
          st == 200 and "teamMembers" in res and "participations" in res, raw[:200])

    st, _, _ = call("GET", f"/api/startups/{victim['id']}", admin)
    check("silinen girişim kartı 404", st == 404, str(st))

    st, after, _ = call("GET", "/api/startups?pageSize=100", admin)
    check("liste bir eksildi",
          after["totalCount"] == startups["totalCount"] - 1,
          f"{after['totalCount']} vs {startups['totalCount']}")

    st, dl, _ = call("GET", "/api/audit-logs?action=Startup.Delete", admin)
    check("silme izi yazılmış", st == 200 and dl["totalCount"] >= 1, str(st))

# ------------------------------------------------------------------- özet
print(f"\n{'=' * 60}")
print(f"geçen: {ok_count}   düşen: {len(failures)}")
for f in failures:
    print(f"  ✗ {f}")
sys.exit(1 if failures else 0)
