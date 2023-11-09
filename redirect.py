from mitmproxy import ctx
import mitmproxy.http

class Redirect:

  def __init__(self):
    print('Loaded redirect addon')

  def request(self, flow: mitmproxy.http.HTTPFlow):
    if 'mod.io' in flow.request.pretty_host:
      ctx.log.info("pretty_host: %s" % flow.request.pretty_host)
      flow.request.host = "lego.com"

addons = [
  Redirect()
]