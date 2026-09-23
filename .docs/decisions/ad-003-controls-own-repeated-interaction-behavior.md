# AD-003: Controls Own Repeated Interaction Behavior

If the same interaction pattern appears in multiple tests or pages, move it into
a Brinell control or shared platform helper.

**Ruled out:** copy-pasted interaction sequences living in test methods or page
objects.

**Broken when:** the same click/wait/read sequence is written more than once
outside a control.
