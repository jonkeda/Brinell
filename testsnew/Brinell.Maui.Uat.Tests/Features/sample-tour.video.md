# Video: A tour of the sample app

| Field | Value |
| --- | --- |
| Beat | 300 |

## Scene: A name is needed

Scenario: Features/main-page-validation.feature.md#Empty-name-shows-validation-message

The sample greets you by name.

| Step | Say |
| --- | --- |
| And I tap Greet | Tap Greet without a name, |
| Then Greeting should contain "Please enter your name" | and it asks for one. |

## Scene: Filling in the form

Scenario: Features/user-form-basic-input.feature.md#User-can-enter-basic-profile-information

The form takes a few details.

| Step | Say |
| --- | --- |
| And I enter "Ada" into First Name | Type a first name, |
| And I check Terms | accept the terms, |
| And I select "United States" from Country | and pick a country. |

That is the sample app.
