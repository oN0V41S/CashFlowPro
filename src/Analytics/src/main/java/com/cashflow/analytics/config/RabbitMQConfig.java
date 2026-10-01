package com.cashflow.analytics.config;

import org.springframework.amqp.core.*;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class RabbitMQConfig{
    @Value("${analytics.exchange.events:cashflow-exchange}")
    private String eventsExchange;

    @Value("${analytics.queue.transfer-completed:cashflow.transfer-completed}")
    private String transferCompletedQueue;

    // Exchange (Already created by Core Banking)
    @Bean
    public TopicExchange eventsExchange() {
        return new TopicExchange(eventsExchange);
    }

    // Queue for transfer events
    @Bean 
    public Queue transferCompletedQueue() {
        return new Queue(transferCompletedQueue, true, false, false);
    }

    // Binding between exchange and queue with routing key
    @Bean
    public Binding transferCompletedBinding(Queue queue, TopicExchange exchange) {
        return BindingBuilder.bind(queue).to(exchange).with("transfer.completed");
    }

    // JSON converter
    @Bean
    public Jackson2JsonMessageConverter jsonMessageConverter() {
        return new Jackson2JsonMessageConverter();
    }

    @Bean 
    public RabbitTemplate rabbitTemplate(ConnectionFactory connectionFactory) {
        RabbitTemplate template = new RabbitTemplate(connectionFactory);
        template.setMessageConverter(jsonMessageConverter());
        return template;
    }
}