v4.0.5 with single site and multisite supporting, double confirm trim code

different with v4.0.4
1. add delay_sync between singlepath setting,
2. change repower delay time from delay_operation to delay_sync,
3. set M.R.E when spin.

Features:
High precision,
Faster version( ~16s per unit).


v4.0.6 with single site and multisite supporting, double confirm trim code

different with v4.0.5
1. add reconnect buttom in EngTab,
2. For autotrim_singlesite(), add a judgement that if any master bit be trimmed trim agian.
3. binXaccuracy set to double.
4. update PreTrim tab binXaccuracy when start up.

v4.0.7 with IT6512 current supply automatic control

different with v4.0.6
1. IT6512 current supply automatic control,
2. add adcoffset in AutoTab,
3. display chosen gain and adcoffset in AutoTab,
4. add IPON and IPOFF in EngTab.

v4.0.8 remove MRE display

different with v4.0.7
1. remove MRE display,
2. restore measure current before autotrim.
3. remove '\r\n' in log

v4.0.9 support multi-site automatic current control

different with v4.0.7
1. multi-site automatic current control
2. change to release version, cause debug version's running speed will be low down after hours.

v4.1.0 add delay during multisite select to overcome some case reload fail.
1. add delay during multisite select,
2. change delay_power to 80ms,
3. retrim if master is 0 when relaod.

*******************************************************************************************************************

v4.2.0 re-struct delay and signal path control
1. add log clear
2. add automatin and manual selection

v4.2.2 change current of power supply output from 2V to 6V for more power ability
1. change current of power supply output from 2V to 6V
2. add 'MOA' and 'MPE' display for signle-site automation trim,
moa---module output abnormal,
mpe---module position error.

