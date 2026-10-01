package com.cashflow.analytics.model;

import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonAlias;
import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import java.math.BigDecimal;
import java.util.UUID;

/* 
    * DTO representing a TransferCompleted event
    * Released for Core Banking (.NET) not RabbitMQ.
*/

@JsonIgnoreProperties(ignoreUnknown = true)
public record TransferEvent(
    @JsonProperty @JsonAlias("FromAccountId") UUID fromAccountId,
    @JsonProperty @JsonAlias("ToAccountId") UUID toAccountId,
    @JsonProperty @JsonAlias("Amount") BigDecimal amount
) {}