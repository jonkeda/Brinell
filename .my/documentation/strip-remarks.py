import re, sys

# Strips /// <remarks> ... </remarks> blocks.
#
# Default: only from members whose declaration carries an explicit non-public modifier
#          (private / internal / protected). Public members, interface members, and any
#          declaration with no explicit modifier are LEFT ALONE (that is phase 3 work, and
#          keeps this safe against interface members that are implicitly public).
# --all:   strip every <remarks> regardless of visibility (the original whole-file behavior).
#
# See .my/documentation/plan.md, phase 2.

MOD_RE = re.compile(r'\b(public|private|protected|internal)\b')


def is_doc(line):
    return line.lstrip().startswith('///')


def declaration_is_nonpublic(lines, group_end):
    """Find the declaration following a doc-comment group and report if it is non-public."""
    j = group_end
    n = len(lines)
    while j < n:
        s = lines[j].strip()
        if s == '' or s.startswith('[') or s.startswith('#'):
            j += 1
            continue
        break
    if j >= n:
        return False  # unknown -> keep
    m = MOD_RE.search(lines[j])
    if not m:
        return False  # no explicit modifier (interface member / ambiguous) -> keep
    return m.group(1) in ('private', 'internal', 'protected')


def strip(path, strip_all):
    with open(path, 'rb') as f:
        has_bom = f.read(3) == b'\xef\xbb\xbf'
    with open(path, encoding='utf-8-sig', newline='') as f:
        text = f.read()
    nl = '\r\n' if '\r\n' in text else '\n'
    lines = text.split(nl)
    n = len(lines)
    remove = [False] * n
    dropped = 0
    i = 0
    while i < n:
        if not is_doc(lines[i]):
            i += 1
            continue
        start = i
        while i < n and is_doc(lines[i]):
            i += 1
        end = i  # doc-comment group is [start, end)
        if not (strip_all or declaration_is_nonpublic(lines, end)):
            continue
        k = start
        while k < end:
            s = lines[k].strip()
            if re.match(r'^///\s*<remarks[ >]', s) or s == '///<remarks>':
                dropped += 1
                remove[k] = True
                if '</remarks>' in s:
                    k += 1
                    continue
                k += 1
                while k < end and '</remarks>' not in lines[k].strip():
                    remove[k] = True
                    k += 1
                if k < end:
                    remove[k] = True  # the </remarks> line
                    k += 1
                continue
            k += 1
    if dropped:
        out = [lines[x] for x in range(n) if not remove[x]]
        with open(path, 'w', encoding='utf-8-sig' if has_bom else 'utf-8', newline='') as f:
            f.write(nl.join(out))
    return dropped


def main(argv):
    strip_all = '--all' in argv
    paths = [a for a in argv if not a.startswith('--')]
    total = 0
    for p in paths:
        m = strip(p, strip_all)
        total += m
        if m:
            print(f'{m:3d}  {p}')
    print(f'{total} blocks removed ({"all" if strip_all else "non-public only"})')


if __name__ == '__main__':
    main(sys.argv[1:])
