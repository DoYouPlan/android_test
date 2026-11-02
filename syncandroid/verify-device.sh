#!/usr/bin/env bash
set -euo pipefail
APP=ru.hryushki.accounts.mobile
RUNNER=$APP.test/androidx.test.runner.AndroidJUnitRunner
adb install -r hryushki/app/build/outputs/apk/debug/app-debug.apk
adb install -r hryushki/app/build/outputs/apk/androidTest/debug/app-debug-androidTest.apk
run_test() {
  adb shell am instrument -w -e class "$1" "$RUNNER" | tee "dist/$2.txt"
  if ! grep -q 'OK (' "dist/$2.txt"; then
    adb logcat -d > dist/device-failure.log
    exit 1
  fi
}
run_test ru.hryushki.accounts.AccountingTest accounting-tests
run_test ru.hryushki.accounts.UiCapture ui-edit-and-capture
for n in 0 1 2 3 4 5; do
  adb exec-out run-as "$APP" cat "files/screen-$n.png" > "dist/screen-$n.png"
done
run_test 'ru.hryushki.accounts.PersistenceCheck#seed' persistence-seed
adb shell am force-stop "$APP"
adb shell am start -W -n "$APP/ru.hryushki.accounts.MainActivity"
run_test 'ru.hryushki.accounts.PersistenceCheck#verify' persistence-after-force-stop
adb shell am start -W -n "$APP/ru.hryushki.accounts.MainActivity"
adb shell screencap -p /sdcard/hryushki-preview.png
adb pull /sdcard/hryushki-preview.png dist/preview.png
adb logcat -d -s AndroidRuntime:E > dist/runtime-errors.txt
