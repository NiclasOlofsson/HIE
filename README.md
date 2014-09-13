HIE
=======

The Healthcare Integration Engine (HIE) is a yet another integration engine with special focus on healthcare connectivity. It attempts to merge good architectural concepts from both *Mirth Connect* as well as *Microsoft BizTalk* in an attempt to provide a next generation integration engine and integration IDE for healthcare interface development.

Technology aside, the most important aspect of the HIE project is really to provide a user friendly IDE that can both work for seasoned developers as well as people ignorant to coding.

## Implemenation Status
HIE is running CI through the fantastic service of AppVeyor. Currently the build status of master is...

[![Build status](https://ci.appveyor.com/api/projects/status/8qby6xf9f4v81sku/branch/master)](https://ci.appveyor.com/project/NiclasOlofsson/hie/branch/master)

_HIE is in no way close to release_, and at this time, not even useful to anyone. It is still in prototyping phase even on it's design and implemenation, so any documentation found here WILL change and should be considered as discussion on "future plans". Feedback and input is very welcome, and this is also why it is up on GitHub already at this early stage of development.

## Runtime model
The HIE engines runtime model is rather straightforward and borrows concepts from both BizTalk and Mirth Connect. BizTalk primarily provide HIE with the notion of *applications* and *endpoints*, while Mirth Connect provide us with the transformation model that is far easier to grasp than orchestration in BizTalk. Below is a breif description of the entities and their roles and responsiblities.

### Application
Each project is represented by an *Application* entity. This is the top-level entity that you deploy into the HIE engine. All endpoints, channels, scripts and assemblies related to a solution is contained within this entity.

### Endpoint
An endpoint represent both a physical location (URI) as well as the processing pipeline logic required for communicating to/from HIE.

Examples of the types of endpoints available in HIE:
- File reader/writer
- TCP receiver/sender
- MSMQ receiver/sender
- LLP
- ASTM 1381/LIS-1

An endpoint also contain a pipeline object responsible for encoding and deconding incoming/outgoing messages, as well as adapting these to the message structure of HIE.
Examples of pipelines supported by HIE:
- NoOp transcoding
- ER transcoding
- JSON transcoding
- XML transcoding

These pipelines converts to and from XML representations necessary in order to transform messages in HIE.

### Channel
A channel is similar in concept to BizTalk orchestration but less flexible. Most of the transformations in healthcare only require simple forward going transformation or very simple request/response scenarios.
A channel will have exactly one source, and one or many destinations.

### Source
The source for a channel has the ability to filter and transform incoming messages before handed off to one or many destinations. A filter or transform set on the source is in all practicallity actually a channel filter and a channel transformation.

### Destination
Once the source has filterd and transformed a message it is routed to the destinations of the channel. Each destination has it's own set of filters and transformers. Each destination also have a target endpoint where the final message will be routed.

### Filter
Filters are used to control which messages will be processed in a source or a specific destination. A filter applied to a source serves as a channel filter, and a filter applied to a destination controls routing within the channel.
Filters are configured as either *acccept* or *deny* logical constructs based on either message properties or content within the body of the message itself. In BizTalk this is usually on properties contained in the message property bag, but in Mirth it is based on content within the message. HIE supports both styles of routing.

### Transformer
If you are coming from a BizTalk world you would see this as the "mapper" or pure XSLT (like the pro do). It would be fair to say that a waste majority of the large scale usages of BizTalk are solemnly based on routing scenarios, somthing that BizTalk excel at, especially when orchestration is a must. Mirth Connect is not really good at routing. It can do most scenarios imaginable, but it is primarly a forward going workflow with little to none real routing going on. However, unlike BizTalk usage, the wast majority of Mirth Connect projects would be 110% focused on transformation. And Mirth Connects "list of transformations" approach is very straightforward and easy to understand and use. However, there are differences in the approach taken by BizTalk and Mirth Connect that deserv some thinking.
BizTalk transformations are conceptually (and technically) based on a source schema (XSD) and a target schema (XSD). In the background, this translate to XSLT regardless if you use the graphical mapping or not (and most pro do not). This approach makes complex mapping rather straightfoward, that is, if you know XSLT very well.
Mirth Connect transforms content based on JavaScripting. In order to simply the scripting environment they lean heavily on the functionality of E4X (http://en.wikipedia.org/wiki/ECMAScript_for_XML). This is nobel since the aim is NOT to use any scripting. But due to Mirth Connect approach to non-schema based concept around transformation mappings, it can't deal with iterative schemas with unknown numbers of repeating segements. Unfortunatly, most messaging in healthcare deal with exactly that scenario. So that means you will need to know JavaScript, and you will in addition also need to understand E4X, something that is completely beyond scope for many people working in healthcare integration since they are usually "occational interface developers" and many do not do this full time. They also tend to have a rather clinical/technical background, in best case from healthcare IT, but very few with programming knowledge, experience or even skills. Part from having difficulties doing JavaScripting, Mirth Connects actual environment for mapping is a disaster from an IDE perspective.


