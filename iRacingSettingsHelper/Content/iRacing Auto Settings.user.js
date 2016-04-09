// ==UserScript==
// @name			iRacing Auto Settings
// @author			Tim Mawson
// @namespace		http://userscripts.org/users/sonofmaw
// @copyright		(c) 2014 Tim Mawson
// @version			1.0
// @match			http://members.iracing.com/membersite/member/*
// @exclude			http://members.iracing.com/membersite/member/EventResult*
// @exclude			http://members.iracing.com/membersite/member/JoinedRace.do
// ==/UserScript==

// Inject our main function into the document as a script, so that we get access to the site's globals and jQuery.
//
(function() {
  var script = document.createElement("script");
  script.textContent = "(" + main.toString() + ")();";
  document.body.appendChild(script);
})();


// iRacing Auto Settings
//
function main() {

	function requestSettingsChange(role) {
		var raceButton = $('#racingpanel_sessionstatus > div > div.racingpanel_button,#racingpanel_sessionstatus > div > a.racepanel_btn,#testingpanel_sesion > a.racepanel_btn, #racingpanel_session > div > div > a.racepanel_btn').first(),
			req = new XMLHttpRequest();
		
		if (role) {
			try {
				req.open('GET', 'http://localhost:52028/?role=' + role, false);
				req.send(null);
								
				if (req.status === 200) {
					raceButton.find('.racingpanel_status_button').text("Settings updated");
				} else {
					raceButton.find('.racingpanel_status_button').text("Settings update failed");
				}
			} catch (e) {
				raceButton.find('.racingpanel_status_button').text("Settings server not found");
			}
		}
	}
	
 
    function createHookedLaunchSession(role, handler, argument) {
        return function() { 
            requestSettingsChange(role);
       
            handler.call(IRACING.control_panels, argument);
        };
    }
	
	function createHookedReplayRequest(handler) {
		return function(index) {
			requestSettingsChange("Watch");
			
			handler.call(document, index);
		};
	}
    
	console.log("iRacing Auto Settings");
	
	IRACING.control_panels.action_join.onclick = createHookedLaunchSession("Join", LaunchSession);
	IRACING.control_panels.action_watch.onclick = createHookedLaunchSession("Watch", LaunchSession);
	IRACING.control_panels.action_spot.onclick = createHookedLaunchSession("Watch", LaunchSession);
	
	IRACING.control_panels.testdrive_test.onclick = createHookedLaunchSession("Join", IRACING.control_panels.testDrive, 'testing');
	IRACING.control_panels.testdrive_race.onclick = createHookedLaunchSession("Join", IRACING.control_panels.testDrive, 'racing');
	
	if ($('#replays_table').length > 0) {
		replayRequest = createHookedReplayRequest(replayRequest);
	}
}