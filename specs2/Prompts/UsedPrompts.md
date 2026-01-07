AI do not use this. Its for human purposes only.

## 001_spx

Create a 001_Spx.Spx.md file with the Spx block information for a connected edition. With the values as based on Brinel

## 002_Idea

recreate the 002_Idea.spx.md

It should be a description of the main idea behind Brinell. Not one specific idea.

## 110_Goals

now in specs2
create the 110_goal block documents
based on the requirements specs and implementation

## 100_Requirements

create the 100_requirements blocks for

REQ-002-non-functional-requirements.md

## 120_Functional.md

in folder specs2 create functional specs blocks

as described in: 120_Functional.md

and based on REQ-001-functional-requirements.md

## 120_Functional.md

### Changes

Make the following changes (you may rephrase if you think it is needed )

changes to 120_003
FR-003.4
Pages can call the wait for page readiness and availability in during creation (constructor) and a parameter can switch this default bavior off.

Changes to 120_004
FR-004.1
Is methods should always return nullable boolean. If the element doesn't exist then a null should be returned.

changes to 120_005
add:
Only in very exceptional cases a wait should be after something. Always try to find a way to wait for something instead.

add: asynchronous operations can also be implemented

change to 120_009
add:
It should be possible to pass a parameter to the test startup which will only start one application ( for instance for html )
The test should then first put the application in a default setting. If this fails the application must be stopped and restarted.

### Reverse Review

Now do a reverser review

in folder Reviews create documents that

Show that all requirements are documented that are defined in the SPEC-006* documents
Are documented that are implemented in SRC

### Add

add the the 120_functional

1. Add the Async Blazor pattern as a generic manner. No need to describe the classes. Only if it doesn yet fit another requirements

## Add requirements for

we need to add some requirements.

Each technology has

its own sample app
These apps should have each control available that will have an ControlObject.
Unit tests must be created. with mocks
The folder samples already contains Full blazor and Maui apps. And partial stride, winforms, and wpf

The folder tests has examples of the V1 implementation of the unit and uitests for Maui, Blazor and core.

These can be used as examples but are not always following the requirements. So should not be copied with out checking against requirements.

## Plan architecture and specifications

in specs2\plan create two planning documents on how we should now create the architecture and specifications base on the SPX v7 blocks

do it highlevel

i want the implementation to be in multiple levels. So to start with only a few controlobjects ( max five ) to check if the first implementation and understanding of the specis is basically corrrect.
and then add more and more controlobjects
But each time that we only have to add code and not really refactor. So base class hierarchies must already be good.

The definition of the architecture must be complete. But the specifications can be by level.

## 200_Architecture

### Review

in folder reviews could you make a review doc of the architecture and make sure it is based on all the requirements in spec2\100\Requirements

## 203

Changes for:

203_001

The corelayer should also contain the crosscutting concerns. Logging etc.

design rules:
No technology specific implementation code.

Als make requirements changes to make this clearer

Interface design principles
The examles do not follow the requirements. timeout and nullable types for instance.

5.3 Composition Over Inheritance

Use inheritance and base classes where possible.

Namespace structure
Do not use the ControlObject6 name. This was old. ( just remove no need to document )

## 211

211_002

6. Platform-Specific Base Classes
   6.1 MAUI (Appium)

In this example you are using casting to AppiumTestContext
and passing the AppiumTestContext instead of an interface.

create things like IMauiTestContext, IBlazorContext and pass those

    protected override object FindElement()
    {
        return ((SeleniumTestContext)_context).FindElement(_locator);
    }

and check if you need to change other documents to make this clear req and arch

## 211_003 and 211_004

the modules 211_003 and 211_004 have a lot of mistakes. Have a good look at the existing implementation of for instance:

Brinell.Maui.UITests.ControlObject6


### stop

stop. Except for 211_003 and 211_004 mainly follow the requirements and architecture documents. AN duse the implementation as example.

You know what. First write an Issue document with what you learned and what you are plannign to do.
