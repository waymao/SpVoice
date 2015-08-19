create table IT_TASK_SMS
(
  sid         VARCHAR2(50) default sys_guid(),
  mission_id  VARCHAR2(50) default 90003,
  f_code      VARCHAR2(10),
  tx_username VARCHAR2(20),
  tx_bmbm     VARCHAR2(20),
  main_sid    VARCHAR2(50),
  node_id     VARCHAR2(50),
  clr         VARCHAR2(100),
  createddate VARCHAR2(20) default TO_CHAR(sysdate, 'yyyy-MM-dd hh24:mi:ss')
)