"""
Fake Crowd Control server for testing the TCG Card Shop Simulator mod.

The mod connects OUT to 127.0.0.1:51337 (the real Crowd Control app listens there),
so this script listens on that port instead. Close the real CC app first.

Usage:
  python tools/cc_test_server.py lights spawn give_100          # send specific effect codes
  python tools/cc_test_server.py --all                          # send every code found in src/ControlClient.cs
  python tools/cc_test_server.py --all --delay 3 --duration 5000

Load a save in the game and be in the shop before running. Each request waits for the
mod's response and prints the status. Timed effects also print their STOP message when they end.
"""
import argparse
import json
import os
import re
import socket
import time

STATUS = {0: "SUCCESS", 1: "FAILURE", 2: "UNAVAIL", 3: "RETRY", 5: "START", 6: "PAUSE", 7: "RESUME", 8: "STOP",
          0x80: "VISIBLE", 0x81: "NOTVISIBLE", 0x82: "SELECTABLE", 0x83: "NOTSELECTABLE", 253: "GAMEUPDATE", 255: "KEEPALIVE"}


def load_codes():
    here = os.path.dirname(os.path.abspath(__file__))
    src = open(os.path.join(here, "..", "src", "ControlClient.cs"), encoding="utf-8-sig").read()
    body = src[src.index("Delegate = new Dictionary"):src.index("public bool isReady()")]
    codes = []
    for line in body.splitlines():
        line = line.strip()
        if line.startswith("//"):
            continue
        m = re.match(r'\{"([^"]+)",\s*CrowdDelegates\.', line)
        if m:
            codes.append(m.group(1))
    return codes


class Conn:
    def __init__(self, sock):
        self.sock = sock
        self.buf = b""
        self.sock.settimeout(0.2)

    def send(self, obj):
        self.sock.sendall(json.dumps(obj).encode("ascii") + b"\0")

    def recv_all(self):
        msgs = []
        try:
            data = self.sock.recv(65536)
            if not data:
                raise ConnectionError("game disconnected")
            self.buf += data
        except socket.timeout:
            pass
        while b"\0" in self.buf:
            raw, self.buf = self.buf.split(b"\0", 1)
            if raw.strip():
                try:
                    msgs.append(json.loads(raw))
                except json.JSONDecodeError:
                    print("  <unparseable>", raw[:200])
        return msgs


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("codes", nargs="*")
    ap.add_argument("--all", action="store_true")
    ap.add_argument("--delay", type=float, default=2.0, help="seconds between effects")
    ap.add_argument("--duration", type=int, default=5000, help="ms duration to request for timed effects")
    ap.add_argument("--wait", type=float, default=8.0, help="seconds to wait for a response")
    ap.add_argument("--port", type=int, default=51337)
    ap.add_argument("--skip-ready", action="store_true", help="send effects even if the game is not in a loaded save (expect RETRY)")
    args = ap.parse_args()

    codes = load_codes() if args.all else args.codes
    if not codes:
        ap.error("give effect codes or --all")

    srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    srv.bind(("127.0.0.1", args.port))
    srv.listen(1)
    print(f"Listening on 127.0.0.1:{args.port}, waiting for the game (mod reconnects every 10s)...")
    sock, addr = srv.accept()
    conn = Conn(sock)
    print("Game connected from", addr)

    # ask for game state until it reports Ready
    for _ in range(60):
        conn.send({"id": 1, "type": "253", "code": ""})
        state = None
        t0 = time.time()
        while time.time() - t0 < 2:
            for m in conn.recv_all():
                if m.get("type") == 253:
                    state = m.get("state")
        print("  game state:", state)
        if state == 1 or args.skip_ready:
            break
        print("  not ready (load a save and focus the game window)...")
        time.sleep(3)

    results = {}
    req_id = 100
    for code in codes:
        req_id += 1
        conn.send({"id": req_id, "code": code, "type": "1", "viewer": "tester", "duration": args.duration})
        got = None
        t0 = time.time()
        while time.time() - t0 < args.wait and got is None:
            for m in conn.recv_all():
                st = m.get("status")
                if st == 255:
                    continue
                if m.get("id") == req_id:
                    got = m
                elif st == 8:
                    print(f"    (timed effect id {m.get('id')} stopped)")
                elif m.get("type") == 1:
                    print(f"    (visibility update {m.get('code')} -> {STATUS.get(st, st)})")
        name = STATUS.get(got.get("status"), got.get("status")) if got else "NO RESPONSE"
        msg = (got or {}).get("message", "")
        results[code] = name
        print(f"{code:45} -> {name} {msg}")
        time.sleep(args.delay)

    print("\nSummary:")
    for k in sorted(set(results.values())):
        print(f"  {k}: {sum(1 for v in results.values() if v == k)}")
    bad = [c for c, v in results.items() if v != "SUCCESS"]
    if bad:
        print("  non-success:", ", ".join(bad))


if __name__ == "__main__":
    try:
        main()
    except ConnectionError as e:
        print(f"\n{e}: the game closed the connection (did it quit, or did the real Crowd Control app grab the port?)")
        raise SystemExit(1)
