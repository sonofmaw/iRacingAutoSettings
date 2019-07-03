// ==UserScript==
// @name      iRacing Auto Settings
// @author      Tim Mawson
// @description     Auto-settings script
// @copyright    (c) 2014, 2019 Tim Mawson
// @license         MIT
// @version      1.9
// @include      https://members.iracing.com/membersite/member/*
// @exclude      https://members.iracing.com/membersite/member/EventResult*
// @exclude      https://members.iracing.com/membersite/member/JoinedRace.do
// ==/UserScript==
//
// ==OpenUserJS==
// @author SonOfMaw
// ==/OpenUserJS==

// iRacing Auto Settings

var script = document.createElement("script");
script.textContent = "(" + iRacingAutoSettings.toString() + ")();";
document.body.appendChild(script);

function iRacingAutoSettings() {
    function requestSettingsChange(role) {
        var raceButton = $(".brand-success");
        req = new XMLHttpRequest();

        if (role) {
            console.log("Changing settings for", role);
            try {
                req.open('GET', 'http://localhost:52028/?role=' + role, false);
                req.send(null);

                if (req.status === 200) {
                    raceButton.text("Settings updated");
                }
                else {
                    raceButton.text("Settings update failed");
                }
            }
            catch (e) {
                raceButton.text("Settings server not found");
            }
        }
    }

    function createHookedLaunchSession(role, handler, argument) {
        return function () {
            requestSettingsChange(role);

            if (argument == "testing") {
                IRACING.control_panels.countdown_dt = 0;
            }

            handler.call(IRACING.control_panels, argument);
        };
    }

    function createHookedReplayRequest(handler) {
        return function (index) {
            requestSettingsChange("Watch");

            handler.call(document, index);
        };
    }

    console.log("iRacing Auto Settings");

    // Latest build have disconnected the LaunchSession function from the race panel Join button, which now uses a local-scope anonymous function that's hidden from us here. 
    if (IRACING.control_panels) {
        console.log("Hooked checkForSingleJoin");

        var oldCheckForSingleJoin = IRACING.control_panels.checkForSingleJoin;
        IRACING.control_panels.checkForSingleJoin = function () {
            var that = this;

            $.getJSON(contextpath + "/member/GetRegisteredSession?invokedBy=racepanel").done(function (data) {
                setTimeout(function () {
                    that.showRegistrationStatus(decodeAllFields(data));
                    var joinBtn = $(".brand-success");
                    if (joinBtn.length) {
                        var role = ["Join", "Watch", "Watch", "Watch", "Watch"][data.userrole];
                        console.log("Hooked button for", role);

                        var clickEvent = jQuery._data(joinBtn[0]).events.click[0];
                        var oldHandler = clickEvent.handler;
                        clickEvent.handler = createHookedLaunchSession(role, oldHandler, joinBtn[0]);
                    }
                }, 100);
            }).error(function () {
                setTimeout(function () {
                    that.showRegistrationStatus({});
                }, 100);
            });
        };

        IRACING.control_panels.checkForSingleJoin();
    }

    if (typeof LaunchSession !== "undefined") {
        var oldLaunchSession = LaunchSession;
        LaunchSession = function () {
            requestSettingsChange(["Join", "Watch", "Watch", "Watch", "Watch"][racingpaneldata.session.userrole]);
            oldLaunchSession();
        };

        IRACING.control_panels.testdrive_test.onclick = createHookedLaunchSession("Join", IRACING.control_panels.testDrive, 'testing');
        IRACING.control_panels.testdrive_race.onclick = createHookedLaunchSession("Join", IRACING.control_panels.testDrive, 'racing');

        if ($('#replays_table').length > 0) {
            replayRequest = createHookedReplayRequest(replayRequest);
        }
    }
}

//@ sourceURL=iRacingAutoSettings.js
