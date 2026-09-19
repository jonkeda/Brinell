# Todo - manual test charters

Tier 5 of the [test pyramid](../README.md#the-five-tiers). These are the checks that stay manual
because a machine cannot judge them, or cannot set them up deterministically. They are not a
copy of the automated tests. Run them per release, on a real phone and on Windows, against a
deployed `Todo.Api`.

Each charter has a time box and a mission. Write down what you find. When a finding can be
reproduced, push it down into an automated tier and record where it went, in the charter's
**Moved down** line. A charter whose findings all moved down can be retired.

The coverage report (`Brinell.Samples.Todo.JourneyCoverage`) checks that every subtask
`journeys.md` marks **M** is named here.

---

## CH-1 Network lost mid-sync - TOD.08.6

**Time box:** 30 min. **Where:** an Android phone with the app and a reachable `Todo.Api`.

**Mission:** Make sure a sync that loses the network halfway never loses or duplicates a todo.

- Make five local changes (create, edit, delete, a status change, one more create). Start Sync,
  and switch on airplane mode while it runs. Try this at several moments.
- Switch the network back on and sync again. Compare the app with the server: `GET /api/todos`.
- Repeat on a slow network (Wi-Fi with a weak signal, or throttled through developer options).

**Look for:** a todo on only one side, a duplicate, a pending marker that never clears, an error
message that does not say what to do.

**Why manual:** the loss must happen at a physical moment in a real radio stack. The
deterministic parts are automated: a timeout keeps changes pending (TOD.08.4, integration), and
a 500 keeps local data (TOD.08.1, integration and hermetic UI).

**Moved down:** -

## CH-2 App killed during save - TOD.08.7

**Time box:** 20 min. **Where:** Android and Windows.

**Mission:** A killed app has either saved an edit or not; it never keeps half an edit.

- Edit a todo's title and notes, tap Save, and kill the app at once (swipe it away on Android;
  Task Manager on Windows). Start it again and open the todo.
- On Android, turn on "Don't keep activities" (developer options) and switch apps while the
  edit page is open. Come back: is the edit still there, and does Cancel still ask?
- Rotate the phone on the edit page with unsaved changes.

**Look for:** a title from the new edit next to notes from the old one; an edit page that forgot
its changes after rotation; a crash on return.

**Why manual:** killing the process at a chosen moment is not repeatable in a UI test. A save is
one SQLite statement, so it lands whole or not at all. The integration tier checks this with a
reader that watches saves from another connection (TOD.08.7).

**Moved down:** -

## CH-3 Looks and accessibility - TOD.10.4, TOD.10.2

**Time box:** 30 min. **Where:** Android (small screen and large) and Windows.

**Mission:** The app is readable and usable in dark mode, at large font sizes and on a small
screen.

- Dark mode: can you tell the status glyphs apart (○ ◐ ● ⚠)? Is the pending marker visible?
- Font scaling at 200 % (Android) or text size 225 % (Windows): do the titles wrap or truncate
  sensibly? Is the status still visible in every row?
- A small screen (5") with the soft keyboard up on the edit page: can you reach `Notes` and Save
  without closing the keyboard?
- TalkBack or Narrator (TOD.10.2): do the rows read as "title, due date, status"? Do the Next
  and Delete buttons, the Sync button and the filter say what they do?

**Look for:** clipped text, low contrast, controls under the keyboard, controls read as
"button" with no name.

**Why manual:** these are visual and usability judgements. The automated Reviews gate checks
that every screen loads with its controls and ids, and saves a screenshot per screen for this
review (TOD.10.1-3).

**Moved down:** -

## CH-4 Exploratory: two devices, one backend

**Time box:** 45 min. **Where:** two devices (or a phone and Windows) on one `Todo.Api`.

**Mission:** Try to create duplicates or lose an edit.

- Edit the same todo on both devices, then sync them in either order. The newer `UpdatedAt`
  should win (TOD.07.6). Does the losing device show the winner after its next sync?
- Delete a todo on one device while the other edits it. Sync both.
- Create todos with the same title on both devices.

**Look for:** a todo that comes back after a delete, an edit that disappears without a trace,
two rows for one todo.

**Why manual:** it is exploratory, and looks for problems nobody has written down yet. The merge
rules are a unit test table (TOD.07.6), and idempotent sync is an integration test (TOD.07.7).

**Moved down:** -

## CH-5 What the Android head skips

**Time box:** 20 min. **Where:** the Android emulator or a phone.

**Mission:** Run by hand the hermetic UI tests that the Android head skips (see the README).

- Filter the list by status, open a todo, and go back: the filter is kept (TOD.01.4, TOD.03.4).
- The edit page title says "New todo" or "Edit todo" (TOD.04.1).
- Todos survive closing and reopening the app (TOD.09.3).
- Airplane mode: Sync says "Offline" and is disabled; edits keep their pending marker (TOD.08.8).
- Edit a todo without syncing: its row shows the pending marker (TOD.01.6), and its detail's
  Sync card says "Waiting to sync" (TOD.03.3).
- Stop the server and restart the app: the local todos are still shown (TOD.09.4).

**Why manual:** until the Android driver can reach the Picker and the Shell title, and can seed
the device's database, these run only on Windows. Retire this charter item by item as they are
automated on Android.

**Moved down:** -
