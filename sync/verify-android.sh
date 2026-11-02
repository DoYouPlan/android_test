#!/usr/bin/env bash
set -euo pipefail
APP=ru.hryushki.accounts.synctest
adb install -r syncandroid/app/build/outputs/apk/debug/app-debug.apk
adb install -r syncandroid/app/build/outputs/apk/androidTest/debug/app-debug-androidTest.apk
for cls in AccountingTest SyncTest; do
 adb shell am instrument -w -e class "ru.hryushki.accounts.$cls" "$APP.test/androidx.test.runner.AndroidJUnitRunner" | tee "sync-dist/$cls.txt"
 if ! grep -q 'OK (' "sync-dist/$cls.txt"; then adb logcat -d > sync-dist/failure.log; exit 1; fi
done
adb shell am start -W -n "$APP/ru.hryushki.accounts.MainActivity"
adb shell screencap -p /sdcard/sync-preview.png
adb pull /sdcard/sync-preview.png sync-dist/preview.png
adb logcat -d -s AndroidRuntime:E > sync-dist/runtime-errors.txt
if grep -q 'FATAL EXCEPTION' sync-dist/runtime-errors.txt; then exit 1; fi
