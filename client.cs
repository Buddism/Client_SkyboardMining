$SkyboardMining::InventoryHeaderString = "<color:0E6D52>Grass \c6-"; //the first string that occurs when typing /inv; todo: think its now grass
package Client_SkyboardMiningPackage
{
	function mouseFire(%x)
	{
		if(!$SkyboardMining::UseDoubleClickHoldFire || !$SkyboardMining::Active)
		{
			return parent::mouseFire(%x);
		}

		if($Sim::Time - $SkyboardMining::LastClick < 0.15 && !%x)
			return;

		if(!%x)
			$SkyboardMining::LastClick = $Sim::Time;

		parent::mouseFire(%x);
	}
	function clientCmdMessageBoxOKCancel(%title, %message, %okServerCmd, %cancelServerCmd)
	{
		switch$(%title)
		{
			case "Upgrade Depth":  $SkyboardMining::lastUpgradeDepthBox = deTag(%message);
			case "Craft this Item": $SkyboardMining::lastItemCraftBox   = deTag(%message);
		}
		$SkyboardMining::lastUpgradeBox = deTag(%message);

		return parent::clientCmdMessageBoxOKCancel(%title, %message, %okServerCmd, %cancelServerCmd);
	}
	function clientCmdMessageBoxYesNo(%title, %message, %okServerCmd, %cancelServerCmd)
	{
		switch$(%title)
		{
			case "Upgrade Depth":  $SkyboardMining::lastUpgradeDepthBox = deTag(%message);
			case "Craft this Item": $SkyboardMining::lastItemCraftBox   = deTag(%message);
		}
		$SkyboardMining::lastUpgradeBox = deTag(%message);

		return parent::clientCmdMessageBoxYesNo(%title, %message, %okServerCmd, %cancelServerCmd);
	}
	function clientCmdServerMessage(%msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10)
	{
		if(!$SkyboardMining::Active)
		{
			return parent::clientCmdServerMessage(%msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10);
		}
		%realMsg = deTag(%msgString);
		if(striPos(%realMsg, $SkyboardMining::InventoryHeaderString) != -1)
		{
			deleteVariables("$SkyboardMining::Inventory*");
			$SkyboardMining::ReadingInventory = $Sim::Time;
		} else if(striPos(%realMsg, "\c6Total cash:\c2") != -1)
		{
			Skyboard_updateMessageHud();
			$SkyboardMining::ReadingInventory = 0;
			if($Sim::Time < $SkyboardMining::HideInventoryUntil)
				return;
		}

		if($Sim::Time - $SkyboardMining::ReadingInventory < 0.5 
			&& getCharCount(%realMsg, "-") == 2
			&& strncmp(%realMsg, "<color:", 7) == 0)
		{
			%strippedMsg = stripMLControlChars(%realMsg);
			%tokenStr = nextToken(%strippedMsg, "oreName", "-");
			%quantity = getWord(%tokenStr, 1) + 0;
			$SkyboardMining::Inventory[trim(%oreName)] = %quantity;
			
			if($Sim::Time < $SkyboardMining::HideInventoryUntil)
				return;
		}
		
		return parent::clientCmdServerMessage(%msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10);
	}
	function clientCmdCenterPrint(%message, %time)
	{
		if($SkyboardMining::Active && stripos(getRecord(%message, 4), "Currently Held: ") != -1)
		{
			%oreName = StripMLControlChars(getRecord(%message, 0));
			%quantity = atoi(StripMLControlChars(getWord(getRecord(%message, 4), 2)));
			//no longer necessary in v1.15
			%addOne = false; //StripMLControlChars(getWord(getRecord(%message, 4), 3)) $= "(+1)";
			
			Skyboard_updateInventory(%oreName, %quantity + %addOne);
		}
		return parent::clientCmdCenterPrint(%message, %time);
	}

	function NMH_Type::send(%this)
	{
		%message = %this.getValue();
		%command = firstWord(%message);
		if(!$SkyboardMining::Active
			&& %command !$= "/SkyboardMiningToggle" && %command !$= "/SMToggle" 
			&& %command !$= "/SkyboardMiningHelp"   && %command !$= "/SMHelp" )
		{
			parent::send(%this);
			return;
		}

		switch$(%command)
		{
			case "/SMReload":
				exec("./client.cs");
				newChatHud_AddLine("\c6Reloading Client_SkyboardMining");
				
			case "/SkyboardMiningToggle" or "/SMToggle":
				$SkyboardMining::Active = !$SkyboardMining::Active;
				if($SkyboardMining::Active)
				{
					newChatHud_AddLine("\c6Client_SkyboardMining is now \c3active! /SMHelp for commands!");
					if(!isObject($SkyboardMining::MessageHud))
					{
						$SkyboardMining::MessageHud = new GuiMLTextCtrl(: newChatText);
					}
					$SkyboardMining::MessageHud.resize(getWord(getRes(), 0) / 2, 20, getWord(getRes(), 0) / 2, 20);
					playGui.add($SkyboardMining::MessageHud);
				} else {
					newChatHud_AddLine("\c6Client_SkyboardMining is now \c0un-active!");
					playGui.remove($SkyboardMining::MessageHud);
				}
			case "/goal" or "/g":
				Skyboard_Command_Goal(%message);

			case "/lud" or "/ld":
				MessageBoxOK("Upgrade Depth (HISTORY)", $SkyboardMining::lastUpgradeDepthBox, "");

			case "/li":
				MessageBoxOK("Craft this Item (HISTORY)", $SkyboardMining::lastItemCraftBox, "");
			
			case "/sinv":
				commandToServer('inv');
				$SkyboardMining::HideInventoryUntil = $Sim::Time + 3;

			case "/ud": commandToServer('upgradeDepth');
			case "/ui": commandToServer('upgradeInventory');
			case "/s":  commandToServer('spawn');
			case "/SkyboardMiningHelp" or "/SMhelp":
				newChatHud_AddLine("\c3/SkyboardMiningToggle \c7or \c3/SMToggle\c6 - toggle the mod");
				newChatHud_AddLine("\c3/goal \c7or \c3/g\c6 - main command cmd (try /goal help)");
				newChatHud_AddLine("\c3/lud \c7or \c3/ld\c6 - last upgrade depth (history)");
				newChatHud_AddLine("\c3/li\c6 - last upgrade item (history)");
				newChatHud_AddLine("\c3/sinv\c6 - silent /inv, mainly for updating hud quantities");
				newChatHud_AddLine("\c3/ud\c6 - short for /upgradeDepth");
				newChatHud_AddLine("\c3/ui\c6 - short for /upgradeInventory");
				newChatHud_AddLine("\c3/s\c6 - short for /spawn");
				newChatHud_AddLine("\c3/SMReload\c6 - reload the mod");
				newChatHud_AddLine("\c3/SkyboardMiningHelp \c7or \c3/SMhelp\c6 - help");

			default: %didNotRunCommand = true;
		}

		//clear the message text & parent so we close the chat like normal supressing actual server commands
		if(!%didNotRunCommand)
		{
			%this.setValue("");
		}
		parent::send(%this);
	}
};
activatePackage(Client_SkyboardMiningPackage);


function Skyboard_Command_Goal(%message)
{
	%type = getWord(%message, 1);
	switch$(%type)
	{
		case "add" or "a":
			%refreshInventory = true;
			%oreQuantity = getWord(%message, getWordCount(%message) - 1);
			if(%oreQuantity $= atoi(%oreQuantity))
			{
				%specifiedQuantity = %oreQuantity;
				%message = removeWord(%message, getWordCount(%message) - 1);
			}

			%oreName = getWords(%message, 2);
			if(%oreName $= "")
			{
				newChatHud_AddLine("empty ore name");
				return;
			}

			if(%specifiedQuantity > 0)
			{
				$SkyboardMining::OreGoal[$SkyboardMining::OreGoalCount + 0] = %oreName TAB %specifiedQuantity;
			} else {
				$SkyboardMining::OreGoal[$SkyboardMining::OreGoalCount + 0] = %oreName;
			}
			
			$SkyboardMining::OreGoalCount++;

			Skyboard_updateMessageHud();
		case "clear" or "c":
			$SkyboardMining::OreGoalCount = 0;
			Skyboard_updateMessageHud();
		case "list" or "li":
			for(%i = 0; %i < $SkyboardMining::OreGoalCount; %i++)
			{
				newChatHud_AddLine(%i SPC $SkyboardMining::OreGoal[%i]);
			}
		case "lock" or "lo":
			%upgradeInfo = $SkyboardMining::lastUpgradeBox;
			for(%i = 2; %i < getRecordCount(%upgradeInfo); %i++)
			{
				%record = StripMLControlChars(getRecord(%upgradeInfo, %i));
				%oreName = getWords(%record, 3);
				commandToServer('lock', %oreName);
			}
		case "lastupgrade" or "lu" or "u":
			%refreshInventory = true;
			$SkyboardMining::OreGoalCount = 0;
			%upgradeInfo = $SkyboardMining::lastUpgradeBox;
			for(%i = 2; %i < getRecordCount(%upgradeInfo); %i++)
			{
				%record = StripMLControlChars(getRecord(%upgradeInfo, %i));
				%specifiedQuantity = getWord(%record, 2);
				%oreName = getWords(%record, 3);
				if(%oreName $= "" || %specifiedQuantity !$= atoi(%specifiedQuantity))
					continue;
					
				$SkyboardMining::OreGoal[$SkyboardMining::OreGoalCount + 0] = %oreName TAB %specifiedQuantity;
				$SkyboardMining::OreGoalCount++;
			}
			Skyboard_updateMessageHud();
		default:
			newChatHud_AddLine("/goal [add/clear/list]");
	}
	
	if(%refreshInventory)
	{
		commandToServer('inv');
		$SkyboardMining::HideInventoryUntil = $Sim::Time + 3;
	}
}

function Skyboard_updateInventory(%oreName, %quantity)
{
	%oreName = trim(StripMLControlChars(%oreName));
	$SkyboardMining::Inventory[%oreName] = %quantity;
	for(%i = 0; %i < $SkyboardMining::OreGoalCount; %i++)
	{
		if(%oreName $= getField($SkyboardMining::OreGoal[%i], 0))
			Skyboard_updateMessageHud();
	}
}

function Skyboard_updateMessageHud()
{
	%text = "<color:ffffff>\c6";
	for(%i = 0; %i < $SkyboardMining::OreGoalCount; %i++)
	{
		%oreName = $SkyboardMining::OreGoal[%i];
		if(getFieldCount(%oreName) > 1)
		{
			%specifiedQuantity = getField(%oreName, 1);
			%oreName = getField(%oreName, 0);
			%quantity = $SkyboardMining::Inventory[%oreName] + 0 @ "/" @ %specifiedQuantity;

			if(%quantity >= %specifiedQuantity)
				%oreName = "\c3" @ %oreName;
		} else {
			%quantity = $SkyboardMining::Inventory[%oreName] + 0;
		}

		%text = %text @ (%i > 0 ? "\n" : "") @ "[" @ %quantity @ "] " @ %oreName;
	}
	$SkyboardMining::MessageHud.setText(%text);
}


function SkyboardMining_removePickSound()
{
	%count = getDataBlockGroupSize();
	for(%I = 0; %I < %count; %I++)
	{
		%db = getDatablock(%I);
		if(%db.getClassName() $= "AudioProfile")
		{
			if(stripos(%db.filename, "Add-Ons/Server_mining/tools/") != -1)
				%db.filename = "";
		}
	}
}
