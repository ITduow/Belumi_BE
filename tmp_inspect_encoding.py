from pathlib import Path

text = Path("belumi_app/lib/config/i18n/app_strings.dart").read_text(encoding="utf-8")
for line in text.splitlines():
    if "'home'" in line or "'login'" in line or "'news'" in line:
        print(ascii(line))
