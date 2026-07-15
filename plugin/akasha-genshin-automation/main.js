// Phase 1 companion vertical slice. Feature commands are added in later phases.

function onLoad() {
    log.info("正在启动 Akasha Automation Worker...");

    companion.start()
        .then(function (startResult) {
            if (!startResult.success) {
                log.error("Worker 启动失败: " + startResult.error);
                return;
            }

            return companion.invoke("worker.echo", {
                source: plugin.id,
                message: "companion-ready"
            });
        })
        .then(function (echoResult) {
            if (!echoResult) {
                return;
            }

            if (echoResult.success) {
                log.info("Akasha Automation Worker 已连接");
            } else {
                log.error("Worker Echo 验证失败: " + echoResult.error);
            }
        })
        .catch(function (error) {
            log.error("Worker 初始化异常: " + error);
        });
}

function onUnload() {
    companion.stop().catch(function (error) {
        log.warn("Worker 停止异常: " + error);
    });
}
