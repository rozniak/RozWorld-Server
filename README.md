# RozWorld-Server
RozWorld's server implementation library.

## How to use
If you are writing your own dedicated server system or otherwise require to run a normal vanilla RozWorld server, this library provides a full implementation ready to go.

You'll need to add this library as well as the **RozWorld-API**, after doing so, you should create a new instance of RwServer(), create an instance of your own logger implemented from the ILogger() interface in the API and set the RwServer's logger to it. After that, simply use RozWorld.SetServer() in the API to set the active server instance and then call the Start() function to begin.

## Building
Download the project, open the solution file, *make sure that references to the RozWorld API and Oddmatics IO libraries work and that they're the correct versions*, then just build with F6 in whatever configuration you want.