HIE
=======

The **Healthcare Integration Engine (HIE)** is a yet another integration engine with special focus on healthcare connectivity. It attempts to merge good architectural concepts from both *Mirth Connect* as well as *Microsoft BizTalk* in an attempt to provide a next generation integration engine and integration IDE for healthcare interface development.

Technology aside, the most important aspect of the HIE project is really to provide a user friendly IDE that can both work for seasoned developers as well as people ignorant to coding.

## Implemenation Status
**HIE is in no way close to release**, and at this time, probably not even useful to anyone. It is still in prototyping phase even on it's design and implemenation, so any documentation found here WILL change and should be considered as discussion on "future plans". Feedback and input is very welcome, and this is also the reason it is up on GitHub even at this early stage of development (part from me not paying for private repository).

HIE is running CI through the fantastic service of AppVeyor. Currently the build status of master is...

[![Build status](https://ci.appveyor.com/api/projects/status/8qby6xf9f4v81sku/branch/master)](https://ci.appveyor.com/project/NiclasOlofsson/hie/branch/master)


## Runtime model
The HIE engines runtime model is rather straightforward and borrows concepts from both BizTalk and Mirth Connect. BizTalk conceptually provide HIE with the notion of *applications* and *endpoints*, while Mirth Connect provide us with the transformation model, which is far easier to grasp than orchestration in BizTalk. 

Below is a breif description of the entities in the runtime model of HIE, together with a conceptual description of their roles and responsiblities.

### Application
Each project is represented by an *Application* entity. This is the top-level entity that you deploy into the HIE engine. All endpoints, channels, scripts and assemblies related to a solution is contained within this entity. This is straight conceptual rip-off from BizTalk. Mirth Connect organizational structure has really nothing to offer in this arena.

It is anticipated that the application will also serve as a domain separator/scope for assemblies deployed within a project.

### Endpoint
An endpoint represent both a physical location (URI) as well as the processing pipeline logic required for communicating to/from HIE.

Examples of the types of endpoints that would be available in HIE...

- File reader/writer
- TCP receiver/sender
- MSMQ receiver/sender
- LLP
- ASTM 1381/LIS-1
- HTTP listeners (REST web services)
- HTTP clients (REST web services)
- WCF services (.NET specific stuff, SOAP and web service in general)

An endpoint also contain a pipeline object responsible for encoding and decoding incoming/outgoing messages, as well as adapting these to the message structure of HIE, as well as setting router properties on messages.

Examples of pipelines that would be supported by HIE...

- NoOp transcoding (also called RAW or binary)
- ER7 transcoding (HL7 v2.x and ASTM1394/LIS-2)
- JSON transcoding
- XML transcoding

These pipelines converts to and from ~~XML~~ [Edit] some representations necessary in order to transform messages in HIE.

~~At this time, it is not yet decided how the endpoints will bind to channels. Publish subscribe, yes sure. But if it will be configured on the endpoint, or on the channel remains to be seen.~~
[EDIT] It is decided to follow the publish/subscribe model as implemented in BizTalk. Should provide the biggest flexibility and also easiest to implement.

### Channel
A channel is similar in concept to BizTalk orchestration, just a bit less flexible. Most of the transformations in healthcare only require simple forward going transformation or very simple request/response scenarios.

A channel will have exactly one source, and one or many destinations.

[EDIT] Currently considering making Channel a *contract* (read Interface) in order to allow both a Mirth approach as well as a workflow approach to orchestration.

### Source
The source for a channel has the ability to filter and transform incoming messages before handed off to one or many destinations. A filter or transform applied to the source is in all practicallity a channel filter and a channel transformation. 
[EDIT] In the publish/subscribe messaging flow the source filters decide whetever a channel will accept the message or not. However, in Mirth Connect the source is also responsible for setting up the endpoint which is not the case with HIE. This might lead to the removal of Source completely and have the filters applied to Channel directly (KISS).

### Destination
Once the source has filterd and transformed a message it will be routed to the destinations of the channel. Each destination has it's own set of filters and transformers. ~~Each destination also have a target endpoint where the final message will be routed.~~
[EDIT] True publish/subscribe choosen. Hence routing will be through properties, no direct addressing. May be changed again at a later stage if deemed necessary.

### Filter
Filters are used to control which messages will be processed in a source or a specific destination. A filter applied to a source serves as a channel filter, and a filter applied to a destination controls routing within the channel.

Filters are configured as either *acccept* or *reject* logical constructs based on either message properties or content within the body of the message itself. In BizTalk this is based on *promoted properties* following the message. Mirth based the logic on the content within the message. HIE will support both styles of routing, but promoted property approach will always be more effecient.

### Transformer
If you are coming from a BizTalk world you would see this as the "mapper" or pure XSLT (like the pro do). It would be fair to say that a wast majority of the large scale usages of BizTalk are routing scenarios. This is somthing that BizTalk excel at (at least in comparison to Mirth). Especially when orchestration is a must. 

Mirth Connect is not really good at routing. It can do most scenarios imaginable, but it is primarly a forward going workflow with little to none real routing going on. However, unlike BizTalk usage, the vast majority of Mirth Connect projects would be 110% focused on transformation. And Mirth Connects "list of transformations" approach is very straightforward - both  easy to understand and easy to use. However, there are differences in the approach taken by BizTalk and Mirth Connect that deserv some attention.

BizTalk transformations are conceptually (and technically) based on a source schema (XSD) and a target schema (XSD). In the background, this translate to XSLT regardless if you use the graphical mapping or not (and most pro don't). This approach makes complex mapping rather straightfoward, that is, if you know XSLT very well.

Mirth Connect transforms content based on JavaScripting (but can do XSLT as well). In order to simply the scripting environment they lean heavily on the functionality of E4X (http://en.wikipedia.org/wiki/ECMAScript_for_XML). Due to Mirth Connect approach to non-schema based concept around transformation mappings, it can't deal with iterative schemas with unknown numbers of repeating segements without scripting. 

Unfortunatly, most messaging in healthcare deal with exactly that (repeating segments) scenario. So that means you will need to know JavaScript, and in addition you also need to understand E4X. If you would go the BizTalk way, it would mean intimit knowledge of XSLT.

This is usally completely beyond scope for many people working in healthcare integration since they are usually "occational interface developers". Many do not do interfacing full time. They also tend to have a rather clinical/technical background, and only in the best of cases from healthcare IT. Very few have real coding knowledge, experience or even skills. Part from having difficulties doing JavaScripting, Mirth Connects actual development environment is an absolut disaster from an IDE perspective.

## Filters & Transformers options
Thanks to .NET wide availability of script-environments it is possible to offer many different ways to create filters and transformers. At this time the following _to be supported_ technologies have been identified:

- JavaScript
- TypeScript
- C#
- Ruby
- Phyton
- XSLT
- JSON based transformations

As an alternative strict declarative transformations and filtering, a HIE specific expression model would also work. Primarily to provide usability for people not in the category of "script kids". This style of transformations would suppoort both **value based mapping** as well as **type based mapping** making it possible to configure support for unknown repeated segments in a message.

##IDE for HIE
At the end of the day, HIE is really about the integrated development environment for interface (integration) development, and a lot about usability.

BizTalk makes heavy use of Visual Studio to provide a basically unlimited support and experience, *for developers*.

Mirth Connect has an "administrator" built into the deployment of a server instance. "Administrator" hints on where this software started, since as an IDE it leaves much to wish for. It is not uncommon for a Mirth developer to have 2-3 instances of the administrator open, and also the command line interface to execute some commands easier.

The state of union right now is that BizTalk is too difficult/scary for non-developers, and Mirth Connect leaves everything to wish for a developer. But at the end of the day, there are parts of the Mirth administrator that makes it win over BizTalk anyday, mostly centered around how it deals with filtering (routing) and transformations.

The idea of the IDE for HIE is to use a platform similar to Visual Studio, but scaled down and "to the point" for novice users. And at the same time, provide the full power of Visual Studio for the users that so wish. **Suggestions for this platform is more than welcome and would be appreciated**. Right now Visual Studio extensions, Visual Studio Shell, #develop, Eclipise are all prospects to service as the IDE for HIE.

## I am Learning!

This whole things is a learning process (obviously), for me personally. If you want to follow that (My) learning process, follow this README but also this document: https://github.com/NiclasOlofsson/HIE/blob/master/Hie/Biz%20Talk%20-%20Mirth%20Connect%20-%20JavaScripting%20-%20Design%20Notes.txt

## Branching strategy

Will be using the Gitflow workflow https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow/ for this eventually, but right now history is in _master_.

