# Video script: how the Todo app works

Status: **draft**, 2026-09-29. A script for the narrator ([plan.md](plan.md)): each scene is a
scenario of `samples/Todo/tests/Brinell.Samples.Todo.Uat`, run and recorded, with its voice-over.
The spoken lines are written as the `Say:` lines the plan proposes, so the recording can be made
from the feature files once they carry them.

- **Length**: about 6 minutes, 11 scenes.
- **Voice**: calm, second person, present tense; one short sentence per step.
- **On screen**: the app beside the Presenter bar (App window: Beside the bar), Speed: Normal; the
  step as a caption at the bottom.
- **Data**: each scene starts from its scenario's test set (`TestData`), so the app shows the same
  todos every time.

## Scene 1: Opening the app (0:00 to 0:35)

Scenario **TOD.01.1** Opening the app shows the list; a loading state shows first, then the content.

| Step | Say |
| --- | --- |
| *(intro)* | This is Todo: a small app to keep track of what you have to do. It works on Windows, Android and the web, and keeps your todos on your device. |
| Given I am on the Todo List page | The app opens on your list. |
| And the server also has todos | The server has a todo you have not seen here yet. |
| And the server answers after 2 seconds | The server is a little slow today. |
| When I tap Sync | You tap Sync to fetch it. |
| Then the list should show the Loading state | While the app waits, it tells you it is loading. |
| And the list should show the Content state | And when the answer arrives, the list shows it. |
| And Todos should have todo "Buy bread" | There it is: Buy bread. |

## Scene 2: Reading the list (0:35 to 1:05)

Scenario **TOD.01.2** Each row shows its title, due date and status.

| Step | Say |
| --- | --- |
| *(intro)* | Every row tells you three things at a glance. |
| Given the synced list has todos | Here are two todos. |
| Then the list should show "Post the letters" and "Paint the fence" | Each row shows its title, |
| *(the due date step)* | when it is due, |
| *(the status step)* | and how far along it is: open, in progress or done. |

## Scene 3: Filtering (1:05 to 1:30)

Scenario **TOD.01.4** Filtering by status shows only the matching rows.

| Step | Say |
| --- | --- |
| *(intro)* | When the list grows, the filter keeps it short. |
| *(pick a status)* | Pick a status, |
| *(the list shows only those rows)* | and only those todos stay. |

## Scene 4: Adding a todo (1:30 to 2:10)

Scenarios **TOD.02.1**, **TOD.02.2**.

| Step | Say |
| --- | --- |
| *(intro)* | Adding a todo takes a few seconds. |
| When I tap Add | Tap Add. |
| Then the edit form is empty, with status Open | You get an empty form; a new todo starts as Open. |
| When I enter a title and save | Give it a title, and save. |
| Then the list shows the new todo | You are back on the list, and your todo is on it. |

## Scene 5: The title is required (2:10 to 2:30)

Scenario Outline **TOD.02.3** An empty or whitespace title shows "Title is required" and does not save.

| Step | Say |
| --- | --- |
| *(intro)* | A todo needs a title. |
| *(save with an empty title)* | Try to save without one, |
| *(the error shows, nothing is saved)* | and the app tells you, right at the field. Nothing is saved. |

## Scene 6: The detail page (2:30 to 3:05)

Scenarios **TOD.03.1**, **TOD.03.3**.

| Step | Say |
| --- | --- |
| *(intro)* | Tap a todo to see all of it. |
| *(tap a row)* | The detail page has three cards: |
| *(the details card)* | the todo itself, |
| *(the notes card)* | your notes, |
| *(the sync card)* | and whether the server has it yet: synced, waiting to sync, or local only. |

## Scene 7: Moving it along (3:05 to 3:35)

Scenarios **TOD.05.1**, **TOD.05.3**.

| Step | Say |
| --- | --- |
| *(intro)* | Moving a todo along is one tap. |
| When I tap Next | Next moves it to the next status, |
| *(it is Done, and saved)* | and it is saved at once. |
| *(an overdue todo)* | A todo past its due date says so, in the list and here. |

## Scene 8: Editing, and changing your mind (3:35 to 4:15)

Scenarios **TOD.04.2**, **TOD.04.4**.

| Step | Say |
| --- | --- |
| *(intro)* | To change more than the status, tap Edit. |
| *(change the title and save)* | Change what you need and save; the list shows it straight away, |
| *(the pending marker)* | marked as waiting to sync. |
| *(change a field, then Cancel)* | Changed your mind? Cancel asks first, |
| *(Discard)* | and Discard leaves the todo as it was. |

## Scene 9: Deleting (4:15 to 4:40)

Scenarios **TOD.06.1**, **TOD.06.2**.

| Step | Say |
| --- | --- |
| *(intro)* | Deleting is safe too. |
| When I tap Delete | Tap Delete, |
| *(the confirmation)* | and the app asks before anything is lost. |
| *(confirm)* | Confirm, and the todo is gone from your list. |

## Scene 10: Sync, and when things go wrong (4:40 to 5:30)

Scenarios **TOD.07.2**, **TOD.08.1**, **TOD.08.2**, **TOD.08.8**.

| Step | Say |
| --- | --- |
| *(intro)* | Your changes live on your device until you sync. |
| *(sync; the pending markers disappear)* | Sync sends them to the server, and the markers disappear. |
| *(the server fails)* | If the server has a problem, you see what went wrong, and a Retry button. Your todos are still here. |
| *(Retry after it recovers)* | Try again when it is back, and you are up to date. |
| *(offline)* | Without a connection, Sync waits and the app says you are offline. You can keep working; your edits sync when you are back online. |

## Scene 11: Your todos stay (5:30 to 6:00)

Scenarios **TOD.09.3**, **TOD.09.4**.

| Step | Say |
| --- | --- |
| *(intro)* | Close the app, and nothing is lost. |
| *(restart)* | Open it again: your todos are where you left them, |
| *(start without the server)* | even when the server cannot be reached. |
| *(outro)* | That is Todo: add, check off, and sync, on any device. |

## Not in this video

The Users pages (TOD.11), the look and accessibility check (TOD.10), and the web and Android heads
make a second video: "Todo for teams". TOD.11 needs the `Using | Users` namespace and the team test
set.

## To make it from this script

1. Put each `Say:` line under its step in the feature files (the rows marked *(step)* above are
   paraphrases; the real step text is taken from the file when the line is added).
2. Order the scenes in a recording list (a `video.md` beside the features: scene, scenario address,
   intro line), which the narrator reads to record several scenarios into one video with chapters.
3. Record from Presenter with the narrator, or `BRINELL_RECORD=1` on the gate run.
