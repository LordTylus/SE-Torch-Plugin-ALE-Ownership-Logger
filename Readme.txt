### Introduction
If you have a server that prohibits PVP or otherwise Griefing you may find yourself in the position where you cannot really track who did or did not do something on a grid.

There may be the possibility to log every interaction with a grid, but that is pretty overkill. In most of the cases people hack blocks, to steal cargo or the whole ship. 

For that I created a Logging tool that logs ownership changes to blocks. 

### How does it work?
Every time a block changes ownership there will be a new log message on the console telling you who owned the block before, who owns it now and what their factions are. 

Since its hard to figure out if the old owner was the one doing the change (for example when removing a block) it has an indication of the old owner was online or offline at that time. 

When a player complains about griefing, you can then use Notepad++ for example to look for that player. And see what grids got changed while he was offline. Maybe you find more entries on that grid that may indicate who took control of it. 

Since v1.0.2.0 Ownership logger has its own Log-File. called ownerships-&lt;Year&gt;-&lt;Month&gt;-&lt;Day&gt;.log and it wont output on the console or torch.log. Both console should not be spammed with unimportant stuff as it makes finding problems harder. At the same time it would be easier for you to look one logfile up instead of scrolling through an infinitely long torch.log

### Optional: Configure NLog (No longer needed/working since v1.0.2.0)

You will notice your console being spammed pretty fast. So I recommend to configure NLog to not log the Messages to the console or torch.log, but to a separate file. 

You can do so by adding:

&lt;target xsi:type="File" name="ownerships" layout="${var:logStamp} ${var:logContent}" fileName="Logs\ownerships-${shortdate}.log"/&gt;

to the targets section of the **NLog-user.config** It
 configures a new output in the logs folder with a output and file format. Every day a new file will be created. 

After that add:

&lt;logger name="OwnershipLogger" minlevel="Debug" writeTo="ownerships" final="true" /&gt;

to the rules section. This tells NLog to output all Messages by the Ownership Logger to the target you configured before.

Finally hit save and restart your server. 

### Examples

**Changes due to taking damage, or griding/welding look like this:**

 19:11:53.6299 [INFO]   Ownership change for block AirVent              from LordTylus [On]            to Nobody               on grid: Vanilla Scout Ship - Tani

 19:11:54.7810 [INFO]   Ownership change for block AirVent              from Nobody [Off]              to LordTylus            on grid: Vanilla Scout Ship - Tani

 19:11:58.4971 [INFO]   Ownership change for block SurvivalKit          from LordTylus [On]            to Nobody               on grid: Vanilla Scout Ship - Tani

 19:12:00.1809 [INFO]   Ownership change for block SurvivalKit          from Nobody [Off]              to LordTylus            on grid: Vanilla Scout Ship - Tani

 19:12:01.4477 [INFO]   Ownership change for block SmallCargoContainer  from LordTylus [On]            to Nobody               on grid: Vanilla Scout Ship - Tani

 19:12:03.1812 [INFO]   Ownership change for block SmallCargoContainer  from Nobody [Off]              to LordTylus            on grid: Vanilla Scout Ship - Tani

 19:12:05.1139 [INFO]   Ownership change for block SmallCargoContainer  from LordTylus [On]            to Nobody               on grid: Vanilla Scout Ship - Tani

 19:12:06.8809 [INFO]   Ownership change for block SmallCargoContainer  from Nobody [Off]              to LordTylus            on grid: Vanilla Scout Ship - Tani

**Changes due to transferring blocks via Terminal look like this:**

19:42:51.0799 [INFO]   Player LordTylus [ALE] requested the following ownership changes on grid: 'Vanilla Scout Ship - Tani'

   block AirVent              from LordTylus [On][ALE]       to [ATF]-Warground     
   block BasicAssembler       from LordTylus [On][ALE]       to [ATF]-Warground     
   block BatteryBlock         from LordTylus [On][ALE]       to [ATF]-Warground     
   block BatteryBlock         from LordTylus [On][ALE]       to [ATF]-Warground     
   block Connector            from LordTylus [On][ALE]       to [ATF]-Warground     
   block ControlPanel         from LordTylus [On][ALE]       to [ATF]-Warground     
   block ProgrammableBlock    from LordTylus [On][ALE]       to [ATF]-Warground     
   block InteriorTurret       from LordTylus [On][ALE]       to [ATF]-Warground     
   block OxygenGenerator      from LordTylus [On][ALE]       to [ATF]-Warground     
   block ProgrammableBlock    from LordTylus [On][ALE]       to [ATF]-Warground     
   block MotorStator          from LordTylus [On][ALE]       to [ATF]-Warground     
   block MotorStator          from LordTylus [On][ALE]       to [ATF]-Warground     
   block MotorStator          from LordTylus [On][ALE]       to [ATF]-Warground     
   block MotorStator          from LordTylus [On][ALE]       to [ATF]-Warground     
   block MotorStator          from LordTylus [On][ALE]       to [ATF]-Warground     
   block MotorStator          from LordTylus [On][ALE]       to [ATF]-Warground     
   block MotorStator          from LordTylus [On][ALE]       to [ATF]-Warground     
   block MotorStator          from LordTylus [On][ALE]       to [ATF]-Warground     
   block AirtightSlide Door   from LordTylus [On][ALE]       to [ATF]-Warground     
   block SmallCargoContainer  from LordTylus [On][ALE]       to [ATF]-Warground     
   block SmallCargoContainer  from LordTylus [On][ALE]       to [ATF]-Warground     
   block SmallCargoContainer  from LordTylus [On][ALE]       to [ATF]-Warground     
   block SmallCargoContainer  from LordTylus [On][ALE]       to [ATF]-Warground     
   block SmallReactor         from LordTylus [On][ALE]       to [ATF]-Warground     
   block SurvivalKit          from LordTylus [On][ALE]       to [ATF]-Warground     
   block Antenna              from LordTylus [On][ALE]       to [ATF]-Warground     
   block RemoteControl        from LordTylus [On][ALE]       to [ATF]-Warground     
   block TimerBlock           from LordTylus [On][ALE]       to [ATF]-Warground     

### Github
[See Here](https://github.com/LordTylus/SE-Torch-Plugin-ALE-Ownership-Logger)