#!/usr/bin/env bash
set -euo pipefail
APP=ru.hryushki.clients
RUNNER=$APP.test/androidx.test.runner.AndroidJUnitRunner
adb install -r crm/app/build/outputs/apk/debug/app-debug.apk
adb install -r crm/app/build/outputs/apk/androidTest/debug/app-debug-androidTest.apk
adb shell pm grant "$APP" android.permission.POST_NOTIFICATIONS
run_test() {
  adb shell am instrument -w -e class "$1" "$RUNNER" | tee "crm-dist/$2.txt"
  if ! grep -q 'OK (' "crm-dist/$2.txt"; then
    adb logcat -d > crm-dist/device-failure.log
    exit 1
  fi
}
run_test ru.hryushki.clients.CrmTest model-tests
run_test ru.hryushki.clients.UiTest ui-tests
for n in 0 1 2 3 4 5; do
  adb exec-out run-as "$APP" cat "files/crm-screen-$n.png" > "crm-dist/screen-$n.png"
done
run_test 'ru.hryushki.clients.PersistenceCheck#seed' seed
adb shell am force-stop "$APP"
adb shell am start -W -n "$APP/.MainActivity"
run_test 'ru.hryushki.clients.PersistenceCheck#verify' persistence
adb install -r crm/app/build/outputs/apk/release/app-release.apk
adb shell am force-stop "$APP"
adb logcat -c
adb shell am start -W -n "$APP/.MainActivity" | tee crm-dist/release-launch.txt
adb shell dumpsys activity activities > crm-dist/release-activity.txt
if ! grep -q 'ru.hryushki.clients/.MainActivity' crm-dist/release-activity.txt; then exit 1; fi
adb shell dumpsys alarm > crm-dist/alarms.txt
if ! grep -q 'ru.hryushki.clients.CHECK' crm-dist/alarms.txt; then exit 1; fi
adb logcat -d -s AndroidRuntime:E > crm-dist/runtime-errors.txt
if grep -q 'FATAL EXCEPTION' crm-dist/runtime-errors.txt; then exit 1; fi
