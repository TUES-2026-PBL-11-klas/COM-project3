from http.server import SimpleHTTPRequestHandler
from socketserver import TCPServer

PORT = 8000

class Handler(SimpleHTTPRequestHandler):
    def log_message(self, format, *args):
        return

with TCPServer(("", PORT), Handler) as httpd:
    print(f"Serving Nexora at http://localhost:{PORT}")
    httpd.serve_forever()
