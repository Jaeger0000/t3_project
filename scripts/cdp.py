"""Bağımlılıksız minimal Chrome DevTools Protocol istemcisi.

Amaç: kimlik doğrulaması gereken ekranları gerçek tarayıcıda render edip
metnini okumak. `--dump-dom` tek başına yetmiyor: oturum jetonu artık HttpOnly
çerezde duruyor (Dalga 2), yani sayfadaki JavaScript onu ne okuyabiliyor ne de
yazabiliyor — çerezi yalnızca CDP yazabilir (bkz. Browser.set_session).
"""

import base64
import json
import os
import socket
import struct
import subprocess
import time
import urllib.request


class WebSocket:
    def __init__(self, url):
        _, rest = url.split("://", 1)
        hostport, path = rest.split("/", 1)
        host, port = hostport.split(":")
        self.sock = socket.create_connection((host, int(port)))
        self.sock.settimeout(30)
        key = base64.b64encode(os.urandom(16)).decode()
        self.sock.sendall(
            f"GET /{path} HTTP/1.1\r\nHost: {hostport}\r\nUpgrade: websocket\r\n"
            f"Connection: Upgrade\r\nSec-WebSocket-Key: {key}\r\n"
            f"Sec-WebSocket-Version: 13\r\n\r\n".encode())
        buf = b""
        while b"\r\n\r\n" not in buf:
            buf += self.sock.recv(4096)
        self.buffer = buf.split(b"\r\n\r\n", 1)[1]

    def _read(self, n):
        while len(self.buffer) < n:
            chunk = self.sock.recv(65536)
            if not chunk:
                raise ConnectionError("bağlantı kapandı")
            self.buffer += chunk
        out, self.buffer = self.buffer[:n], self.buffer[n:]
        return out

    def send(self, text):
        payload = text.encode()
        header = bytearray([0x81])
        length = len(payload)
        if length < 126:
            header.append(0x80 | length)
        elif length < 65536:
            header.append(0x80 | 126)
            header += struct.pack(">H", length)
        else:
            header.append(0x80 | 127)
            header += struct.pack(">Q", length)
        mask = os.urandom(4)
        header += mask
        masked = bytes(b ^ mask[i % 4] for i, b in enumerate(payload))
        self.sock.sendall(bytes(header) + masked)

    def recv(self):
        first = self._read(2)
        opcode = first[0] & 0x0F
        length = first[1] & 0x7F
        if length == 126:
            length = struct.unpack(">H", self._read(2))[0]
        elif length == 127:
            length = struct.unpack(">Q", self._read(8))[0]
        data = self._read(length)
        if opcode == 0x8:
            raise ConnectionError("sunucu kapattı")
        return data.decode(errors="replace")


class Browser:
    def __init__(self, port=9222, profile="/tmp/t3-cdp-profile"):
        subprocess.run(["rm", "-rf", profile], check=False)
        self.proc = subprocess.Popen(
            ["google-chrome-stable", "--headless=new", f"--remote-debugging-port={port}",
             "--no-sandbox", "--disable-gpu", "--disable-dev-shm-usage",
             f"--user-data-dir={profile}", "about:blank"],
            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        target = None
        for _ in range(60):
            try:
                with urllib.request.urlopen(f"http://127.0.0.1:{port}/json/list") as r:
                    pages = [t for t in json.load(r) if t["type"] == "page"]
                if pages:
                    target = pages[0]
                    break
            except Exception:
                pass
            time.sleep(0.5)
        if not target:
            raise RuntimeError("Chrome hedefi bulunamadı")
        self.ws = WebSocket(target["webSocketDebuggerUrl"])
        self.next_id = 0

        # Headless tarayıcı pencereyi "odakta değil" saydığı için `:focus`
        # seçicisi hiç eşleşmiyor: element document.activeElement olsa bile
        # odak stilleri uygulanmıyordu ve klavye erişilebilirliği ölçülemiyordu.
        # Bu emülasyon sayfayı her zaman odakta gösteriyor.
        self.call("Emulation.setFocusEmulationEnabled", enabled=True)

    def call(self, method, **params):
        self.next_id += 1
        request_id = self.next_id
        self.ws.send(json.dumps({"id": request_id, "method": method, "params": params}))
        while True:
            message = json.loads(self.ws.recv())
            if message.get("id") == request_id:
                if "error" in message:
                    raise RuntimeError(f"{method}: {message['error']}")
                return message.get("result", {})

    def set_session(self, token, origin="http://localhost:5173"):
        """Oturumu çerezle kurar (giriş formunu doldurmadan).

        Jeton HttpOnly çerezde olduğu için `localStorage.setItem` yolu kapandı:
        script erişemiyor — güvenlik kazancının kendisi bu. CDP tarayıcının
        kendi çerez deposuna yazdığı için kurulum yine tek satır.

        CSRF çerezi de yazılıyor: yazma istekleri çift-gönderim jetonu istiyor
        ve değer bir sır değil, yalnızca çerez ile başlığın eşleşmesi aranıyor.
        """
        self.call("Network.enable")
        self.call("Network.setCookie", name="t3.session", value=token,
                  url=origin, path="/", httpOnly=True, sameSite="Strict")
        self.call("Network.setCookie", name="t3.csrf", value="render-kontrolu",
                  url=origin, path="/", httpOnly=False, sameSite="Strict")
        # Arayüz "daha önce oturum açıldı mı" bilgisini bu işaretten okuyor;
        # yokken 401 alan ziyaretçiye "süre doldu" demiyor (bkz. AuthProvider).
        self.evaluate("localStorage.setItem('t3.session.active', '1')")

    def clear_session(self):
        """Çerezleri ve yerel işareti siler: oturumsuz durumu sınamak için."""
        self.call("Network.enable")
        self.call("Network.clearBrowserCookies")
        self.evaluate("localStorage.clear()")

    def evaluate(self, expression):
        result = self.call("Runtime.evaluate", expression=expression,
                           returnByValue=True, awaitPromise=True)
        return result.get("result", {}).get("value")

    def goto(self, url, wait_for=None, timeout=20):
        self.call("Page.navigate", url=url)
        deadline = time.time() + timeout
        while time.time() < deadline:
            time.sleep(0.35)
            text = self.evaluate("document.body ? document.body.innerText : ''") or ""
            if wait_for is None and text.strip():
                return text
            if wait_for and wait_for in text:
                return text
            if "Yükleniyor" in text or "yükleniyor" in text:
                continue
        return self.evaluate("document.body ? document.body.innerText : ''") or ""

    def click_text(self, label, wait_for=None, timeout=10):
        """Metnine göre bir düğmeye tıklar (sekmeler rota değil, düğme).

        Seçici yerine metin kullanılıyor: test, kullanıcının gördüğü etikete
        bağlı kalsın — sınıf adı değişince sessizce yeşil kalan bir kontrol
        hiçbir şey doğrulamaz.
        """
        clicked = self.evaluate(
            "(() => {"
            "  const wanted = " + json.dumps(label) + ";"
            "  const el = [...document.querySelectorAll('button, a')]"
            "    .find(n => (n.innerText || '').trim().startsWith(wanted));"
            "  if (!el) return false;"
            "  el.click();"
            "  return true;"
            "})()")

        if not clicked:
            return None

        deadline = time.time() + timeout
        while time.time() < deadline:
            time.sleep(0.3)
            text = self.text()
            if wait_for is None or wait_for in text:
                return text
        return self.text()

    def text(self):
        return self.evaluate("document.body ? document.body.innerText : ''") or ""

    def close(self):
        self.proc.terminate()
