import re, html, sys, urllib.request, time
def fetch(url, out):
    try:
        req = urllib.request.Request(url, headers={'User-Agent':'Mozilla/5.0 (Windows NT 10.0; Win64; x64)'})
        t = urllib.request.urlopen(req, timeout=30).read().decode('utf-8','ignore')
    except Exception as e:
        print('FAIL', url, e); return False
    open(out+'.html','w',encoding='utf-8').write(t)
    t = re.sub(r'<script[\s\S]*?</script>','',t); t = re.sub(r'<style[\s\S]*?</style>','',t)
    txt = html.unescape(re.sub(r'<[^>]+>',' ',t))
    txt = re.sub(r'[ \t\r]+',' ',txt)
    lines=[l.strip() for l in txt.split('\n') if l.strip()]
    open(out+'.txt','w',encoding='utf-8').write('URL: '+url+'\n=====\n\n'+'\n'.join(lines))
    print('OK', out, len(t))
    return True
for u,o in [l.split() for l in open('urls.txt')]:
    fetch(u,o); time.sleep(0.4)
