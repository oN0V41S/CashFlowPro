package com.cashflow.analytics.consumer;

import com.cashflow.analytics.model.TransferEvent;
import com.cashflow.analytics.service.AnalyticsService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;

@Component
public class TransferCompletedConsumer {
    private static final Logger log = LoggerFactory.getLogger(TransferCompletedConsumer.class);

    private final AnalyticsService analyticsService;

    @Autowired
    public TransferCompletedConsumer(AnalyticsService analyticsService) {
        this.analyticsService = analyticsService;
    }

    @RabbitListener(queues = "${analytics.queue.transfer-completed}")
    public void handleTransferCompletedEvent(TransferEvent transferEvent) {
        log.info("Received transfer completed event: de{}, para={}, valor={}", transferEvent.fromAccountId(), transferEvent.toAccountId(), transferEvent.amount());
        analyticsService.processTransfer(transferEvent);

        log.info("✅Event processed successfully");
    }
}
